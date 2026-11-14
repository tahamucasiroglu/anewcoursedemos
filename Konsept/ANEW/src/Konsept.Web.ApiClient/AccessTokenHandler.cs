using System.Net.Http.Headers;
using Konsept.Web.ApiClient.Abstract;

namespace Konsept.Web.ApiClient;

/// <summary>
/// Api isteklerine geçerli kullanıcının erişim token'ını Bearer olarak ekler (istek zaten bir Authorization taşıyorsa
/// dokunmaz). Token kaynağı yoksa ya da kullanıcı giriş yapmamışsa başlık eklenmez. Çerçeve adaptörü (DelegatingHandler).
/// </summary>
/// <param name="source">Token kaynağı (yoksa <c>null</c>).</param>
public sealed class AccessTokenHandler(IAccessTokenSource? source) : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Headers.Authorization is null && source?.GetAccessToken() is { Length: > 0 } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
