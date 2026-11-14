using Microsoft.AspNetCore.Http;
using TahaKonsept.Modules.Agents.Api.Security.Base;
using TahaKonsept.Modules.Agents.Application.Options;

namespace TahaKonsept.Modules.Agents.Api.Security.Concrete;

/// <summary>
/// Sunucu adını o anki HTTP isteğinden alan doğrulama adresi sağlayıcı (ajan API'ye hangi adla geldiyse web de o adda, 7110 portunda).
/// İstek yoksa <c>localhost</c>.
/// </summary>
public sealed class HttpVerificationUrlProvider : VerificationUrlProviderBase
{
    private readonly IHttpContextAccessor _accessor;

    /// <summary>Oluşturur.</summary>
    /// <param name="options">Ayarlar.</param>
    /// <param name="accessor">HTTP bağlam erişimi.</param>
    public HttpVerificationUrlProvider(AgentsOptions options, IHttpContextAccessor accessor)
        : base(options)
    {
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    /// <summary>Şema ve sunucu adı.</summary>
    /// <returns>Şema ve sunucu.</returns>
    protected override (string Scheme, string Host) CurrentHost()
    {
        var request = _accessor.HttpContext?.Request;
        return request is null || !request.Host.HasValue
            ? ("http", "localhost")
            : (request.Scheme, request.Host.Host);
    }
}
