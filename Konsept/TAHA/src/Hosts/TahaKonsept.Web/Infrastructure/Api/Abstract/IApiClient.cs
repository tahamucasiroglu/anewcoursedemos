namespace TahaKonsept.Web.Infrastructure.Api.Abstract;

/// <summary>
/// MVC → API generic HTTP istemcisi. Oturumdaki erişim token'ını Bearer olarak ekler, süresi dolmak üzereyse ya da
/// API 401 dönerse <c>api/identity/refresh</c> ile yeniler ve isteği bir kez tekrarlar.
/// Önemli metotlar: <see cref="GetAsync{T}"/>, <see cref="PostAsync{TRequest, TResponse}"/>,
/// <see cref="PutAsync{TRequest, TResponse}"/>, <see cref="DeleteAsync{TResponse}"/>. Hiçbiri istisna fırlatmaz; sonuç
/// <see cref="ApiCallResult{T}"/> olarak döner.
/// </summary>
public interface IApiClient
{
    /// <summary>GET isteği gönderir.</summary>
    /// <typeparam name="T">Cevap türü.</typeparam>
    /// <param name="url">API köküne göre göreli adres (ör. <c>api/profiles</c>).</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<T>> GetAsync<T>(string url, CancellationToken ct = default);

    /// <summary>JSON gövdeli POST isteği gönderir.</summary>
    /// <typeparam name="TRequest">İstek gövdesi türü.</typeparam>
    /// <typeparam name="TResponse">Cevap türü.</typeparam>
    /// <param name="url">Göreli adres.</param>
    /// <param name="body">İstek gövdesi.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);

    /// <summary>JSON gövdeli PUT isteği gönderir.</summary>
    /// <typeparam name="TRequest">İstek gövdesi türü.</typeparam>
    /// <typeparam name="TResponse">Cevap türü.</typeparam>
    /// <param name="url">Göreli adres.</param>
    /// <param name="body">İstek gövdesi.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest body, CancellationToken ct = default);

    /// <summary>DELETE isteği gönderir.</summary>
    /// <typeparam name="TResponse">Cevap türü (gövde yoksa varsayılan).</typeparam>
    /// <param name="url">Göreli adres.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<ApiCallResult<TResponse>> DeleteAsync<TResponse>(string url, CancellationToken ct = default);
}
