using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Http.Abstract;

/// <summary>
/// Bearer token'lı ajan → API istemcisi. 401'de token bir kez yenilenip istek tekrarlanır.
/// Önemli metotlar: <see cref="GetAsync{TResponse}"/>, <see cref="PostAsync{TRequest, TResponse}"/>, <see cref="DownloadFileAsync"/>.
/// </summary>
public interface IAgentApiClient
{
    /// <summary>GET isteği.</summary>
    /// <typeparam name="TResponse">Yanıt türü.</typeparam>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<TResponse>> GetAsync<TResponse>(string uri, CancellationToken cancellationToken = default);

    /// <summary>POST isteği (yanıt gövdeli).</summary>
    /// <typeparam name="TRequest">İstek türü.</typeparam>
    /// <typeparam name="TResponse">Yanıt türü.</typeparam>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result<TResponse>> PostAsync<TRequest, TResponse>(string uri, TRequest body, CancellationToken cancellationToken = default);

    /// <summary>POST isteği (yanıt gövdesiz).</summary>
    /// <typeparam name="TRequest">İstek türü.</typeparam>
    /// <param name="uri">Göreli adres.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Result> PostAsync<TRequest>(string uri, TRequest body, CancellationToken cancellationToken = default);

    /// <summary>Dosyayı (ör. <c>api/agents/artifacts/{id}</c>) indirir; önce <c>.partial</c> dosyasına yazar, bitince taşır.</summary>
    /// <param name="url">Göreli ya da mutlak adres.</param>
    /// <param name="destinationPath">Hedef dosya.</param>
    /// <param name="progress">0–100 ilerleme (boyut biliniyorsa).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>İndirilen bayt sayısı ya da hata.</returns>
    Task<Result<long>> DownloadFileAsync(string url, string destinationPath, IProgress<double>? progress, CancellationToken ct);
}
