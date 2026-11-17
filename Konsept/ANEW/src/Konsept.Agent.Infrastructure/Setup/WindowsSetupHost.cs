using System.Diagnostics;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using Konsept.Agent.Application.Abstract;

namespace Konsept.Agent.Infrastructure.Setup;

/// <summary>
/// Kurulumun gerçek dış dünyası (spec 0009): HTTP indirme, sürücü boş alanı, SHA-256, zip açma ve aracı pencere açmadan çalıştırma.
/// Yönetici izni gerektiren hiçbir şey yapmaz; ortam/araç yolu ayarlarına dokunmaz (Req 6–7). Çerçeve adaptörü — testlerde sahte.
/// </summary>
/// <param name="http">İndirme istemcisi.</param>
/// <param name="inactivityTimeout">Gövdeden veri gelmeden geçebilecek en uzun süre; verilmezse 30 sn (review r1 M-1).</param>
public sealed class WindowsSetupHost(HttpClient http, TimeSpan? inactivityTimeout = null) : ISetupHost
{
    /// <summary>Varsayılan hareketsizlik süresi: bağlantı sessizce koparsa indirme bu süre sonunda başarısız sayılır.</summary>
    public static readonly TimeSpan DefaultInactivityTimeout = TimeSpan.FromSeconds(30);

    private readonly TimeSpan _inactivity = inactivityTimeout ?? DefaultInactivityTimeout;

    /// <summary>Hazır denetiminde bir aracın en uzun çalışma süresi.</summary>
    public static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>npm bileşen kurulumunun en uzun süresi.</summary>
    public static readonly TimeSpan PackageTimeout = TimeSpan.FromMinutes(10);

    /// <inheritdoc />
    public bool IsOnline() => NetworkInterface.GetIsNetworkAvailable();

    /// <inheritdoc />
    public long FreeBytes(string directory) =>
        new DriveInfo(Path.GetPathRoot(Path.GetFullPath(directory))!).AvailableFreeSpace;

    /// <inheritdoc />
    public async Task DownloadAsync(Uri source, string destination, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(source, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength;
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            var file = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, useAsync: true);
            await using (file.ConfigureAwait(false))
            {
                var buffer = new byte[1 << 16];
                long done = 0;
                int read;
                while ((read = await ReadWithTimeoutAsync(stream, buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    done += read;
                    if (total is > 0)
                    {
                        progress?.Report((double)done / total.Value);
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<string> ComputeSha256Async(string file, CancellationToken cancellationToken)
    {
        var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16, useAsync: true);
        await using (stream.ConfigureAwait(false))
        {
            var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
            return Convert.ToHexStringLower(hash);
        }
    }

    /// <inheritdoc />
    public void ExtractArchive(string archive, string destination) =>
        ZipFile.ExtractToDirectory(archive, destination, overwriteFiles: true);

    /// <inheritdoc />
    public async Task<string?> RunAsync(string executable, string arguments, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(executable, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
        };
        try
        {
            using var process = Process.Start(info);
            if (process is null)
            {
                return null;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(CheckTimeout);
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                return null;
            }

            return process.ExitCode == 0 ? await output.ConfigureAwait(false) + await error.ConfigureAwait(false) : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> InstallPackagesAsync(string componentId, string lockSha256, string nodeDirectory, string destination, CancellationToken cancellationToken)
    {
        foreach (var name in new[] { "package.json", "package-lock.json" })
        {
            await using var source = typeof(WindowsSetupHost).Assembly.GetManifestResourceStream($"Konsept.Agent.Infrastructure.Setup.{componentId}.{name}");
            if (source is null)
            {
                return false;
            }

            using var memory = new MemoryStream();
            await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
            var bytes = memory.ToArray();
            if (name == "package-lock.json" && !string.Equals(LockSha256(bytes), lockSha256, StringComparison.OrdinalIgnoreCase))
            {
                return false; // Kilit dosyası katalogdaki hash'le uyuşmuyor (değiştirilmiş yapı).
            }

            await File.WriteAllBytesAsync(Path.Combine(destination, name), bytes, cancellationToken).ConfigureAwait(false);
        }

        var info = CreateNpmStartInfo(nodeDirectory, destination);
        try
        {
            using var process = Process.Start(info);
            if (process is null)
            {
                return false;
            }

            _ = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            _ = process.StandardError.ReadToEndAsync(CancellationToken.None);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(PackageTimeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        finally
        {
            TryDeleteDirectory(NpmCacheDirectory(destination)); // Paketler kuruldu; indirme önbelleği yer kaplamasın.
        }
    }

    /// <summary>
    /// npm kurulum sürecinin başlangıç bilgisi. npm önbelleği ve günlükleri bileşen klasörünün içinde tutulur: kullanıcının kendi npm
    /// önbelleğine (<c>%LOCALAPPDATA%\npm-cache</c>) yazılmaz, oradaki günlükler silinmez, kaldırmadan sonra iz kalmaz (spec 0009 Req 6–7).
    /// </summary>
    /// <param name="nodeDirectory">Kurulu Node.js klasörü.</param>
    /// <param name="destination">Paketlerin kurulacağı klasör.</param>
    /// <returns>Başlangıç bilgisi.</returns>
    public static ProcessStartInfo CreateNpmStartInfo(string nodeDirectory, string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        var info = new ProcessStartInfo(Path.Combine(nodeDirectory, "node.exe"))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = destination,
        };
        foreach (var arg in new[] { Path.Combine(nodeDirectory, "node_modules", Path.Combine("npm", "bin", "npm-cli.js")), "ci", "--ignore-scripts", "--no-audit", "--no-fund", "--loglevel=error" })
        {
            info.ArgumentList.Add(arg);
        }

        info.Environment["HYPERFRAMES_NO_TELEMETRY"] = "1";
        info.Environment["DO_NOT_TRACK"] = "1";
        info.Environment["npm_config_cache"] = NpmCacheDirectory(destination);
        info.Environment["npm_config_update_notifier"] = "false";
        return info;
    }

    /// <summary>npm'in bu kurulum için kullandığı önbellek klasörü (kurulum bitince silinir).</summary>
    /// <param name="destination">Paketlerin kurulacağı klasör.</param>
    /// <returns>Klasör yolu.</returns>
    public static string NpmCacheDirectory(string destination) => Path.Combine(destination, ".npm-cache");

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Önbellek silinemezse kurulum yine geçerlidir; bileşen klasörüyle birlikte kaldırılır.
        }
    }

    /// <summary>Kilit dosyasının satır sonundan bağımsız SHA-256'sı (CRLF → LF).</summary>
    /// <param name="bytes">İçerik.</param>
    /// <returns>Küçük harf onaltılık.</returns>
    public static string LockSha256(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var text = System.Text.Encoding.UTF8.GetString(bytes).Replace("\r\n", "\n", StringComparison.Ordinal);
        return Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));
    }

    private async Task<int> ReadWithTimeoutAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        idle.CancelAfter(_inactivity);
        try
        {
            return await stream.ReadAsync(buffer, idle.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Bağlantı sessizce koptu (veri yok, hata yok): motor yeniden dener, aynayı dener, sonunda "İnternet bağlantısı koptu" der.
            throw new IOException("İndirme durdu: bağlantıdan veri gelmiyor.");
        }
    }
}
