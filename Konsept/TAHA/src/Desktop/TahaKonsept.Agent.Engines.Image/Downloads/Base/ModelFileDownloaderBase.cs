using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Engines.Image.Downloads.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.Downloads.Base;

/// <summary>
/// Kaldığı yerden devam eden indirici tabanı. Akış: hedef doğru boyutta varsa atla → <c>.part</c> boyutu kadar
/// <c>Range</c> iste (206 ise ekle, 200 ise baştan yaz, 416 ise tamam say) → boyut/SHA-256 doğrula → hedefe taşı.
/// Ağ hatasında <see cref="RetryCount"/> kez yeniden dener. Alt sınıf <see cref="CreateClient"/> ve
/// isteğe bağlı <see cref="PrepareRequest"/> (ör. yetki başlığı) sağlar.
/// </summary>
public abstract class ModelFileDownloaderBase : IModelFileDownloader
{
    private const int BufferSize = 1024 * 1024;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>Taban oluşturur.</summary>
    /// <param name="logger">Günlükleyici.</param>
    protected ModelFileDownloaderBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>Günlükleyici.</summary>
    protected ILogger Logger { get; }

    /// <summary>Toplam deneme sayısı (en az 1).</summary>
    protected abstract int RetryCount { get; }

    /// <inheritdoc />
    public async Task<Result> DownloadAsync(Uri source, string destinationPath, long? expectedSize, string? sha256, IProgress<ModelDownloadProgress>? progress, CancellationToken ct)
    {
        if (File.Exists(destinationPath) && (expectedSize is null || new FileInfo(destinationPath).Length == expectedSize))
        {
            progress?.Report(new ModelDownloadProgress(new FileInfo(destinationPath).Length, expectedSize));
            return Result.Success();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        var partPath = destinationPath + ".part";
        Error lastError = Error.Failure("download.failed", $"İndirilemedi: {source}");

        for (var attempt = 1; attempt <= Math.Max(1, RetryCount); attempt++)
        {
            try
            {
                var transfer = await TransferAsync(source, partPath, expectedSize, progress, ct).ConfigureAwait(false);
                if (transfer.IsFailure)
                {
                    return transfer;
                }

                return await FinalizeAsync(partPath, destinationPath, expectedSize, sha256, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException && !ct.IsCancellationRequested)
            {
                Logger.LogWarning(ex, "İndirme kesildi ({Attempt}/{Total}): {Source}", attempt, RetryCount, source);
                lastError = Error.Failure("download.network", $"İndirme sırasında ağ hatası: {ex.Message}");
            }
        }

        return Result.Failure(lastError);
    }

    /// <summary>İndirmede kullanılacak istemci (zaman aşımı sonsuz olmalı; iptal belirteçle yapılır).</summary>
    /// <returns>İstemci.</returns>
    protected abstract HttpClient CreateClient();

    /// <summary>İstek gönderilmeden önce başlık eklemek için kanca.</summary>
    /// <param name="request">İstek.</param>
    protected virtual void PrepareRequest(HttpRequestMessage request)
    {
    }

    private async Task<Result> TransferAsync(Uri source, string partPath, long? expectedSize, IProgress<ModelDownloadProgress>? progress, CancellationToken ct)
    {
        var existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0L;
        if (expectedSize is not null && existing == expectedSize)
        {
            return Result.Success();
        }

        if (expectedSize is not null && existing > expectedSize)
        {
            File.Delete(partPath);
            existing = 0;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, source);
        if (existing > 0)
        {
            request.Headers.Range = new RangeHeaderValue(existing, null);
        }

        PrepareRequest(request);
        var client = CreateClient();
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            if (expectedSize is null || existing == expectedSize)
            {
                return Result.Success();
            }

            File.Delete(partPath);
            throw new IOException("Sunucu devam aralığını kabul etmedi; baştan indirilecek.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var code = (int)response.StatusCode;
            var message = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? $"Dosyaya erişim reddedildi ({code}). Depo kapılı olabilir; Hugging Face anahtarı gerekebilir: {source}"
                : $"İndirme başarısız ({code}): {source}";
            if (code >= 500)
            {
                throw new HttpRequestException(message);
            }

            return Result.Failure(Error.Failure("download.http", message));
        }

        var append = response.StatusCode == HttpStatusCode.PartialContent && existing > 0;
        if (!append)
        {
            existing = 0;
        }

        var total = expectedSize ?? (response.Content.Headers.ContentLength is { } len ? len + existing : null);
        await using var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var output = new FileStream(partPath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true);

        var buffer = new byte[BufferSize];
        var received = existing;
        var watch = Stopwatch.StartNew();
        progress?.Report(new ModelDownloadProgress(received, total));
        int read;
        while ((read = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            received += read;
            if (watch.Elapsed >= ProgressInterval)
            {
                progress?.Report(new ModelDownloadProgress(received, total));
                watch.Restart();
            }
        }

        progress?.Report(new ModelDownloadProgress(received, total));
        return Result.Success();
    }

    private async Task<Result> FinalizeAsync(string partPath, string destinationPath, long? expectedSize, string? sha256, CancellationToken ct)
    {
        var length = new FileInfo(partPath).Length;
        if (expectedSize is not null && length != expectedSize)
        {
            throw new IOException($"Eksik indirme: {length}/{expectedSize} bayt.");
        }

        if (!string.IsNullOrWhiteSpace(sha256))
        {
            string actual;
            await using (var stream = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
            {
                actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false));
            }

            if (!string.Equals(actual, sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(partPath);
                return Result.Failure(Error.Failure("download.checksum", $"SHA-256 uyuşmadı: {Path.GetFileName(destinationPath)}. Dosya silindi, yeniden indirilmesi gerekiyor."));
            }
        }

        File.Move(partPath, destinationPath, overwrite: true);
        Logger.LogInformation("İndirildi: {Path} ({Bytes} bayt)", destinationPath, length);
        return Result.Success();
    }
}
