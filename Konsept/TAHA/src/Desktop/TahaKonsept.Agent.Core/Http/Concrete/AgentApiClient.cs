using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.Agent.Core.Http.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Http.Concrete;

/// <summary>
/// Bearer token'lı ajan API istemcisi: <see cref="PrepareRequestAsync"/> erişim token'ını ekler,
/// <see cref="TryRecoverUnauthorizedAsync"/> 401'de <see cref="IAgentAuthService.RefreshAsync"/> çağırır.
/// <see cref="DownloadFileAsync"/> artefaktları akış halinde indirir.
/// </summary>
public sealed class AgentApiClient : AgentApiClientBase, IAgentApiClient
{
    private readonly IAgentAuthService _auth;

    /// <summary>Oluşturur.</summary>
    /// <param name="httpClient">HttpClient.</param>
    /// <param name="settings">Ayar deposu.</param>
    /// <param name="auth">Oturum servisi.</param>
    public AgentApiClient(HttpClient httpClient, IAgentSettingsStore settings, IAgentAuthService auth)
        : base(httpClient, settings)
    {
        _auth = auth;
    }

    /// <inheritdoc />
    public async Task<Result<long>> DownloadFileAsync(string url, string destinationPath, IProgress<double>? progress, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        try
        {
            var response = await SendDownloadAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized && await TryRecoverUnauthorizedAsync(ct))
            {
                response.Dispose();
                response = await SendDownloadAsync(url, ct);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    return await ReadErrorAsync(response, ct);
                }

                return await SaveAsync(response, destinationPath, progress, ct);
            }
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure("http.unreachable", "Sunucuya ulaşılamadı: " + ex.Message);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Error.Failure("http.timeout", "İndirme zaman aşımına uğradı.");
        }
    }

    /// <inheritdoc />
    protected override async Task PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await base.PrepareRequestAsync(request, cancellationToken);
        var token = await _auth.GetAccessTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <inheritdoc />
    protected override Task<bool> TryRecoverUnauthorizedAsync(CancellationToken cancellationToken) => _auth.RefreshAsync(cancellationToken);

    private async Task<HttpResponseMessage> SendDownloadAsync(string url, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ToAbsolute(url));
        await PrepareRequestAsync(request, ct);
        return await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
    }

    private static async Task<Result<long>> SaveAsync(HttpResponseMessage response, string destinationPath, IProgress<double>? progress, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var partial = destinationPath + ".partial";
        var total = response.Content.Headers.ContentLength;
        long written = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            await using (var source = await response.Content.ReadAsStreamAsync(ct))
            await using (var target = new FileStream(partial, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                int read;
                while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), ct);
                    written += read;
                    if (total is > 0)
                    {
                        progress?.Report(Math.Min(100d, written * 100d / total.Value));
                    }
                }
            }

            File.Move(partial, destinationPath, overwrite: true);
            progress?.Report(100d);
            return Result.Success(written);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            if (File.Exists(partial))
            {
                TryDelete(partial);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Yarım dosya silinemedi; bir sonraki indirmede üzerine yazılır.
        }
    }
}
