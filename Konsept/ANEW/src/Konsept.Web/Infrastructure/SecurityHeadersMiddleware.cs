namespace Konsept.Web.Infrastructure;

/// <summary>
/// Güvenlik başlıkları (docs/security.md): CSP (inline script ve stil yasak; video yalnızca sitenin kendisinden ve ajanın bu bilgisayardaki
/// yerel akışından — spec 0017 Req 21), MIME koklama yok, çerçeveye gömme yok.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; media-src 'self' http://127.0.0.1:*; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.</summary>
    /// <param name="next">Sonraki adım.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>Başlıkları ekler ve isteği devreder.</summary>
    /// <param name="context">HTTP bağlamı.</param>
    /// <returns>Görev.</returns>
    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        return _next(context);
    }
}
