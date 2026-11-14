using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Api.Results;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.BuildingBlocks.Api.Middleware;

/// <summary>
/// Yakalanmamış istisnaları loglar (Error) ve 500 ProblemDetails (<c>code = server.unhandled</c>) döner;
/// istemci isteği iptal ettiyse 499 yazar ve sessizce geçer. Kayıt: <c>app.UseTahaExceptionHandling()</c>.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    /// <summary>İstemci iptali durum kodu (nginx geleneği).</summary>
    public const int ClientClosedRequest = 499;

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="next">Sonraki middleware.</param>
    /// <param name="logger">Logger.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>İsteği işler.</summary>
    /// <param name="context">İstek bağlamı.</param>
    /// <returns>Görev.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogDebug("İstek istemci tarafından iptal edildi: {Path}", context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ClientClosedRequest;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yakalanmamış hata: {Method} {Path}", context.Request.Method, context.Request.Path);
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            await ProblemDetailsMapper.WriteAsync(context, Error.Failure("server.unhandled", "Beklenmeyen bir hata oluştu."));
        }
    }
}
