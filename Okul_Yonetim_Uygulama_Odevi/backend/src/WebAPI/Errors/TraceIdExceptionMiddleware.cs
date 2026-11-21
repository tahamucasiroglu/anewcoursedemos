using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Extensions;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Handlers;
using NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.Middleware;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Ports;
using NArchitectureLogger = NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction.ILogger;
using NArchitectureValidationProblemDetails = NArchitecture.Core.CrossCuttingConcerns.Exception.WebApi.HttpProblemDetails.ValidationProblemDetails;

namespace OkulYonetim.WebAPI.Errors;

/// <summary>
/// nArchitecture hata middleware'inin bu projeye uyarlanmış hali: her hata izleme numarasıyla bir kez loglanır
/// (beklenmeyen hata Error, iş hataları Warning). İş hatalarının yanıtı nArchitecture'daki gibidir; beklenmeyen
/// hatada yanıt yalnızca genel Türkçe mesaj ve izleme numarası taşır (ayrıntı sızdırılmaz). İş hatası yanıtı her istek için
/// ayrı <c>HttpExceptionHandler</c> ile yazılır (temel sınıfın paylaşılan işleyicisi eşzamanlı hatalarda yanıtları karıştırır). Doğrulama
/// hatasında gövde nArchitecture biçimindedir, yalnızca <c>detail</c> sabit İngilizce metin yerine ilk Türkçe alan mesajıdır (T20). İstemci isteği iptal ettiyse
/// (sayfa kapandı) bu hata sayılmaz: Information olarak loglanır, yanıt yazılmaz.
/// </summary>
public sealed class TraceIdExceptionMiddleware : ExceptionMiddleware
{
    private static readonly string[] Locales = ["tr"];
    private readonly ILogger<TraceIdExceptionMiddleware> logger;

    public TraceIdExceptionMiddleware(
        RequestDelegate next,
        IHttpContextAccessor contextAccessor,
        NArchitectureLogger loggerService,
        ILogger<TraceIdExceptionMiddleware> logger)
        : base(next, contextAccessor, loggerService)
    {
        this.logger = logger;
    }

    protected override Task LogException(HttpContext context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        if (IsClientAbort(context, exception))
        {
            this.logger.LogInformation("İstek istemci tarafından iptal edildi: {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else if (IsExpected(exception))
        {
            this.logger.LogWarning("İş hatası ({ExceptionType}): {Reason}", exception.GetType().Name, exception.Message);
        }
        else
        {
            this.logger.LogError(exception, "Beklenmeyen hata: {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        return Task.CompletedTask;
    }

    protected override Task HandleExceptionAsync(HttpResponse response, dynamic exception)
    {
        ArgumentNullException.ThrowIfNull(response);
        Exception error = (Exception)exception;
        if (IsClientAbort(response.HttpContext, error))
        {
            return Task.CompletedTask;
        }

        if (error is ForbiddenException forbidden)
        {
            return WriteForbiddenAsync(response, forbidden.Message);
        }

        // nArchitecture'ın kendi HandleExceptionAsync'i tek (singleton) HttpExceptionHandler'ın Response özelliğini değiştirir:
        // aynı anda iki iş hatasında yanıtlar karışır (biri iki gövde, öteki boş). Her çağrıya ayrı işleyici; dynamic,
        // temel sınıftaki gibi hata türüne göre aşırı yüklemeyi (BusinessException, ValidationException…) seçtirir.
        if (!IsExpected(error))
        {
            return WriteUnexpectedErrorAsync(response);
        }

        // İşleyici gövdeyi düz metin olarak yazar; içerik türünü temel sınıf ayarlıyordu (application/json) — aynısı.
        response.ContentType = MediaTypeNames.Application.Json;
        if (error is ValidationException validation)
        {
            return WriteValidationAsync(response, validation);
        }

        return (Task)new HttpExceptionHandler { Response = response }.HandleException((dynamic)error);
    }

    /// <summary>
    /// Doğrulama hatası (T20): nArchitecture <c>detail</c>'e sabit İngilizce metin yazar, Türkçe alan mesajları <c>Errors</c>
    /// dizisindedir. Gövde aynı biçimde kalır (alan listesi dokunulmaz), yalnızca <c>detail</c> ilk Türkçe alan mesajı olur —
    /// <c>detail</c>'i okuyan her istemci kullanıcıya Türkçe mesajı gösterir.
    /// </summary>
    private static Task WriteValidationAsync(HttpResponse response, ValidationException validation)
    {
        NArchitectureValidationProblemDetails problem = new(validation.Errors);
        string? first = validation.Errors?
            .SelectMany(e => e.Errors ?? [])
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
        if (first is not null)
        {
            problem.Detail = first;
        }

        response.StatusCode = StatusCodes.Status400BadRequest;
        return response.WriteAsync(problem.ToJson());
    }

    private static async Task WriteForbiddenAsync(HttpResponse response, string message)
    {
        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status403Forbidden,
            Title = message,
            Detail = message,
        };
        problem.Extensions["traceId"] = response.HttpContext.TraceIdentifier;
        response.StatusCode = StatusCodes.Status403Forbidden;
        await response.WriteAsJsonAsync(problem, options: null, contentType: MediaTypeNames.Application.ProblemJson).ConfigureAwait(false);
    }

    private static async Task WriteUnexpectedErrorAsync(HttpResponse response)
    {
        ILocalizationService localization = response.HttpContext.RequestServices.GetRequiredService<ILocalizationService>();
        string message = await localization.GetLocalizedAsync("UnexpectedError", Locales, "Common").ConfigureAwait(false);
        ProblemDetails problem = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = message,
        };
        problem.Extensions["traceId"] = response.HttpContext.TraceIdentifier;

        response.StatusCode = StatusCodes.Status500InternalServerError;
        await response.WriteAsJsonAsync(problem, options: null, contentType: MediaTypeNames.Application.ProblemJson).ConfigureAwait(false);
    }

    private static bool IsClientAbort(HttpContext context, Exception exception) =>
        exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested;

    private static bool IsExpected(Exception exception) =>
        exception is BusinessException or ValidationException or AuthorizationException or ForbiddenException or NotFoundException or ConflictException;
}
