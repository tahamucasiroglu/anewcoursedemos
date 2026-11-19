using System.Diagnostics;
using Serilog.Context;

namespace OkulYonetim.WebAPI.Logging;

/// <summary>
/// Her isteğe tek bir izleme numarası (TraceId) bağlar: .NET'in istek izleme kimliği (Activity) kullanılır; aynı numara
/// log satırlarına, hata yanıtına ve yanıtın "X-Trace-Id" başlığına yazılır. İstek satırının kendisi (yöntem, yol, durum, süre) Serilog istek logu tarafından yazılır; gövde yazılmaz.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    public const string TraceIdHeader = "X-Trace-Id";
    private readonly RequestDelegate next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        string traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.TraceIdentifier = traceId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[TraceIdHeader] = traceId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("TraceId", traceId))
        {
            await this.next(context).ConfigureAwait(false);
        }
    }
}
