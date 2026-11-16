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
