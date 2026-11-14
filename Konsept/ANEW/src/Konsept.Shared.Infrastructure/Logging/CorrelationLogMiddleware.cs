using Konsept.Shared.Logging;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Konsept.Shared.Infrastructure.Logging;

/// <summary>
/// İsteğin korelasyon numarasını (ProblemDetails'teki <c>correlationId</c> ile aynı: <c>TraceIdentifier</c>) istek
/// boyunca yazılan her loga ekler (spec 0008 Req 2). Çerçeve adaptörü (middleware).
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
