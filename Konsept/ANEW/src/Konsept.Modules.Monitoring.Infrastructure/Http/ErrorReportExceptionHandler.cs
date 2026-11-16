using Konsept.Modules.Monitoring.Application.Abstract;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Monitoring.Infrastructure.Http;

/// <summary>
/// API'nin beklenmeyen hatalarından otomatik hata kaydı (spec 0022 Req 10, Ç-6): istek hattına düşen her istisna kaynak "API" ile bir kez
/// kaydedilir (korelasyon = <c>TraceIdentifier</c>, istek oturumluysa kullanıcı, maskeli özet: istisna türü ve mesajı, yığın izi yok).
/// Yanıtı üretmez (<c>false</c> döner) — ProblemDetails aynen sürer. Beklenen hatalar (4xx) istisna olmadığı için kayıt oluşturmaz.
/// Kayıt yazılamazsa (veritabanı yok) yalnızca Warning loglanır; döngü olmaz. Çerçeve adaptörü (<see cref="IExceptionHandler"/>).
/// </summary>
/// <param name="logger">Logger.</param>
public sealed partial class ErrorReportExceptionHandler(ILogger<ErrorReportExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false; // İstemci vazgeçti: hata değil.
        }

        try
        {
            var reports = httpContext.RequestServices.GetRequiredService<IErrorReportService>();
            await reports.RecordUnexpectedAsync(
                $"{exception.GetType().Name}: {exception.Message}",
                httpContext.TraceIdentifier,
                CancellationToken.None).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Otomatik kayıt en iyi çabadır; asıl hata yanıtı engellenmez.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogRecordFailed(logger, ex.GetType().Name);
        }

        return false;
    }

    [LoggerMessage(EventId = 2201, Level = LogLevel.Warning, Message = "API hatası otomatik hata kaydına yazılamadı ({ErrorType})")]
    private static partial void LogRecordFailed(ILogger logger, string errorType);
}
