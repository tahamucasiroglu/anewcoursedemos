using Konsept.Shared.Logging;
using Serilog.Context;

namespace Konsept.Web.Infrastructure;

/// <summary>
/// İsteğin korelasyon numarasını (<c>TraceIdentifier</c>) istek boyunca yazılan her Web loguna ekler (spec 0008
/// Req 1). Çerçeve adaptörü (middleware); Web, Y-5 gereği Api'nin ortak altyapısını kullanmaz.
/// </summary>
/// <param name="next">Sonraki adım.</param>
public sealed class CorrelationLogMiddleware(RequestDelegate next)
{
    /// <summary>İsteği korelasyon numarasıyla işler.</summary>
    /// <param name="context">HTTP bağlamı.</param>
    /// <returns>Görev.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        using (LogContext.PushProperty(KonseptLogging.CorrelationProperty, context.TraceIdentifier))
        {
            await next(context).ConfigureAwait(false);
        }
    }
}
