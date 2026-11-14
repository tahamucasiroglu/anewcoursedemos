using System.Buffers;
using System.Security.Cryptography;
using TahaKonsept.Agent.Core.Downloads.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Downloads.Base;

/// <summary>
/// İndirici tabanı: akış halinde <c>.partial</c> dosyasına yazar, aynı anda <see cref="IncrementalHash"/> ile SHA256 hesaplar,
/// beklenen hash uyuşmazsa dosyayı siler. HTTP istemcisi <see cref="CreateClient"/> ile türetilen sınıftan gelir.
/// </summary>
public abstract class FileDownloaderBase : IFileDownloader
{
    /// <inheritdoc />
    public async Task<Result<DownloadedFile>> DownloadAsync(Uri url, string destinationPath, string? expectedSha256, IProgress<double>? progress, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationPath))!);
        var partial = destinationPath + ".partial";
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            var client = CreateClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                return Error.Failure("agent.download_failed", "İndirilemedi (" + (int)response.StatusCode + "): " + url);
            }

            var total = response.Content.Headers.ContentLength;
            long written = 0;
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var source = await response.Content.ReadAsStreamAsync(ct))
            await using (var target = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    hash.AppendData(buffer, 0, read);
                    await target.WriteAsync(buffer.AsMemory(0, read), ct);
                    written += read;
                    if (total is > 0)
                    {
                        progress?.Report(Math.Min(100d, written * 100d / total.Value));
                    }
                }
            }

            var sha = Convert.ToHexStringLower(hash.GetHashAndReset());
            if (!string.IsNullOrWhiteSpace(expectedSha256) && !string.Equals(sha, expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Error.Failure("agent.download_hash_mismatch", "İndirilen dosya doğrulanamadı (SHA256 uyuşmuyor): " + Path.GetFileName(destinationPath));
            }

            File.Move(partial, destinationPath, overwrite: true);
            progress?.Report(100d);
            return new DownloadedFile(destinationPath, written, sha);
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure("agent.download_failed", "İndirme başarısız: " + ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Error.Failure("agent.download_failed", "İndirme zaman aşımına uğradı: " + url);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            TryDelete(partial);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GetStringAsync(Uri url, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(url);
        try
        {
            var client = CreateClient();
            using var response = await client.GetAsync(url, ct);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync(ct)
                : Error.Failure("agent.download_failed", "Okunamadı (" + (int)response.StatusCode + "): " + url);
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure("agent.download_failed", "Bağlantı hatası: " + ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Error.Failure("agent.download_failed", "Zaman aşımı: " + url);
        }
    }

    /// <summary>HTTP istemcisini döner (uzun indirmeler için zaman aşımı geniş olmalı).</summary>
    /// <returns>İstemci.</returns>
    protected abstract HttpClient CreateClient();

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Bir sonraki indirmede üzerine yazılır.
        }
    }
}
