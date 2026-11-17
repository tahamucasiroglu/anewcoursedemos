using System.Diagnostics;
using System.Formats.Tar;
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
/// <param name="logger">Teknik log (uv kurulum hatasının sebebi; spec 0028 review r1 L-1); verilmezse yazılmaz.</param>
public sealed class WindowsSetupHost(HttpClient http, TimeSpan? inactivityTimeout = null, Serilog.ILogger? logger = null) : ISetupHost
{
    /// <summary>Varsayılan hareketsizlik süresi: bağlantı sessizce koparsa indirme bu süre sonunda başarısız sayılır.</summary>
    public static readonly TimeSpan DefaultInactivityTimeout = TimeSpan.FromSeconds(30);

    private readonly TimeSpan _inactivity = inactivityTimeout ?? DefaultInactivityTimeout;

    /// <summary>Hazır denetiminde bir aracın en uzun çalışma süresi.</summary>
    public static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>npm bileşen kurulumunun en uzun süresi.</summary>
    public static readonly TimeSpan PackageTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Python paket ortamı kurulumunun en uzun süresi (CUDA'lı PyTorch ~2,7 GB indirir; spec 0028).</summary>
    public static readonly TimeSpan PythonTimeout = TimeSpan.FromMinutes(90);

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
    public void ExtractArchive(string archive, string destination)
    {
        ArgumentNullException.ThrowIfNull(archive);
        if (archive.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            // Taşınabilir Python (python-build-standalone) tar.gz yayımlanır (spec 0028).
            Directory.CreateDirectory(destination);
            using var file = File.OpenRead(archive);
            using var gzip = new GZipStream(file, CompressionMode.Decompress);
            TarFile.ExtractToDirectory(gzip, destination, overwriteFiles: true);
            return;
        }

        ZipFile.ExtractToDirectory(archive, destination, overwriteFiles: true);
    }

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

    /// <inheritdoc />
    public async Task<bool> InstallPythonPackagesAsync(
        string componentId,
        string? variant,
        string lockSha256,
        string uvExecutable,
        string pythonExecutable,
        string destination,
        string cacheDirectory,
        CancellationToken cancellationToken)
    {
        var name = PythonLockResource(componentId, variant);
        await using var source = typeof(WindowsSetupHost).Assembly.GetManifestResourceStream(name);
        if (source is null)
        {
            return false;
        }

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        var bytes = memory.ToArray();
        if (!string.Equals(LockSha256(bytes), lockSha256, StringComparison.OrdinalIgnoreCase))
        {
            return false; // Kilit dosyası katalogdaki hash'le uyuşmuyor (değiştirilmiş yapı).
        }

        try
        {
            Directory.CreateDirectory(cacheDirectory);
            var lockFile = Path.Combine(cacheDirectory, "requirements.txt");
            await File.WriteAllBytesAsync(lockFile, bytes, cancellationToken).ConfigureAwait(false);
            var info = CreateUvStartInfo(uvExecutable, pythonExecutable, lockFile, destination, cacheDirectory);
            using var process = Process.Start(info);
            if (process is null)
            {
                return false;
            }

            _ = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var error = process.StandardError.ReadToEndAsync(CancellationToken.None);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(PythonTimeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Süreç tam o anda bitti.
                }

                cancellationToken.ThrowIfCancellationRequested();
                logger?.Warning("Python ortamı kurulumu süre sınırını aştı: {Component}", componentId);
                return false;
            }

            if (process.ExitCode != 0)
            {
                var text = await error.ConfigureAwait(false);
                logger?.Warning("Python ortamı kurulamadı: {Component} uv çıkış {Exit}; çıktının sonu: {Output}", componentId, process.ExitCode, text.Length > 2000 ? text[^2000..] : text);
            }

            return process.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        finally
        {
            TryDeleteDirectory(cacheDirectory); // Paketler kuruldu; uv önbelleği (açılmış tekerlekler, GB'larca) yer kaplamasın.
        }
    }

    /// <summary>Python ortamının gömülü kilit dosyasının kaynak adı (spec 0028): <c>python.&lt;id&gt;[.&lt;varyant&gt;].txt</c>.</summary>
    /// <param name="componentId">Bileşen.</param>
    /// <param name="variant">Varyant ya da <c>null</c>.</param>
    /// <returns>Kaynak adı.</returns>
    public static string PythonLockResource(string componentId, string? variant) =>
        $"Konsept.Agent.Infrastructure.Setup.python.{componentId}{(variant is null ? string.Empty : "." + variant)}.txt";

    /// <summary>
    /// uv kurulum sürecinin başlangıç bilgisi (spec 0028 Req 4): her paket hash'li (<c>--require-hashes</c>), bağımlılık çözümü yok (kilit tam
    /// liste), hedef bileşen klasörü (<c>--target</c>), yalnızca kurulu taşınabilir Python; uv yapılandırma dosyaları ve kullanıcının <c>UV_*</c> /
    /// <c>PIP_*</c> ortam değişkenleri yok sayılır, Python indirmesi kapalı (kayıt defterine yazılmaz), önbellek ajan klasöründe (çalışma klasörü uv'nin klasörü), sistem sertifika deposu.
    /// </summary>
    /// <param name="uvExecutable">uv.</param>
    /// <param name="pythonExecutable">Python.</param>
    /// <param name="lockFile">Kilit dosyası.</param>
    /// <param name="destination">Hedef klasör.</param>
    /// <param name="cacheDirectory">Önbellek.</param>
    /// <returns>Başlangıç bilgisi.</returns>
    public static ProcessStartInfo CreateUvStartInfo(string uvExecutable, string pythonExecutable, string lockFile, string destination, string cacheDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uvExecutable);
        var info = new ProcessStartInfo(uvExecutable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(uvExecutable)!, // Önbellek klasörü çalışma klasörü olamaz (uv reddeder; duman testi).
        };
        foreach (var arg in new[]
        {
            "pip", "install", "--no-config", "--require-hashes", "--no-deps", "--index-strategy", "unsafe-best-match", "--link-mode", "copy",
            "--python", pythonExecutable, "--target", destination, "-r", lockFile,
        })
        {
            info.ArgumentList.Add(arg);
        }

        foreach (var key in info.Environment.Keys.Where(k => k.StartsWith("UV_", StringComparison.OrdinalIgnoreCase) || k.StartsWith("PIP_", StringComparison.OrdinalIgnoreCase)
            || k.Equals("PYTHONPATH", StringComparison.OrdinalIgnoreCase) || k.Equals("PYTHONHOME", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            info.Environment.Remove(key);
        }

        info.Environment["UV_CACHE_DIR"] = cacheDirectory;
        info.Environment["UV_NO_CONFIG"] = "1";
        info.Environment["UV_PYTHON_DOWNLOADS"] = "never";
        info.Environment["UV_NO_PROGRESS"] = "1";
        info.Environment["UV_SYSTEM_CERTS"] = "1"; // Windows sertifika deposu (kurumsal vekil sunucularda HttpClient ile aynı davranış).
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
