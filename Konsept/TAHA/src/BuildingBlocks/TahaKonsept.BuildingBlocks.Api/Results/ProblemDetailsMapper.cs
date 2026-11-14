using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Common;

namespace TahaKonsept.BuildingBlocks.Api.Results;

/// <summary>
/// <see cref="Error"/> → HTTP ProblemDetails dönüşümü (tek yerde). Durum eşlemesi: Validation 400, Unauthorized 401,
/// Forbidden 403, NotFound 404, Conflict 409, Failure 500. Ek alanlar: <c>code</c>, <c>errorType</c>, <c>traceId</c>, <c>errors</c>.
/// </summary>
public static class ProblemDetailsMapper
{
    /// <summary>ProblemDetails içerik türü.</summary>
    public const string ProblemContentType = "application/problem+json";

    /// <summary>Hata türünü HTTP durum koduna çevirir.</summary>
    /// <param name="type">Hata türü.</param>
    /// <returns>HTTP durum kodu.</returns>
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Hatadan ProblemDetails üretir.</summary>
    /// <param name="httpContext">İstek bağlamı.</param>
    /// <param name="error">Hata.</param>
    /// <returns>ProblemDetails (doğrulama hatalarında <see cref="ValidationProblemDetails"/>).</returns>
    public static ProblemDetails Create(HttpContext httpContext, Error error)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(error);

        var problem = error.Details is { Count: > 0 }
            ? new ValidationProblemDetails(error.Details.ToDictionary(p => p.Key, p => p.Value))
            : new ProblemDetails();

        problem.Status = ToStatusCode(error.Type);
        problem.Title = TitleFor(error.Type);
        problem.Detail = error.Message;
        problem.Instance = httpContext.Request.Path;
        problem.Extensions[ProblemExtensionKeys.Code] = error.Code;
        problem.Extensions[ProblemExtensionKeys.ErrorType] = error.Type.ToString();
        problem.Extensions[ProblemExtensionKeys.TraceId] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        return problem;
    }

    /// <summary>MVC için ProblemDetails sonucu üretir.</summary>
    /// <param name="httpContext">İstek bağlamı.</param>
    /// <param name="error">Hata.</param>
    /// <returns>ObjectResult.</returns>
    public static ObjectResult ToObjectResult(HttpContext httpContext, Error error)
    {
        var problem = Create(httpContext, error);
        var result = new ObjectResult(problem) { StatusCode = problem.Status };
        result.ContentTypes.Add(ProblemContentType);
        return result;
    }

    /// <summary>ProblemDetails'i doğrudan yanıta yazar (middleware için).</summary>
    /// <param name="httpContext">İstek bağlamı.</param>
    /// <param name="error">Hata.</param>
    /// <returns>Görev.</returns>
    public static Task WriteAsync(HttpContext httpContext, Error error)
    {
        var problem = Create(httpContext, error);
        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        return httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), ContractJson.Options, ProblemContentType, httpContext.RequestAborted);
    }

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Geçersiz istek",
        ErrorType.Unauthorized => "Oturum gerekli",
        ErrorType.Forbidden => "Yetki yok",
        ErrorType.NotFound => "Bulunamadı",
        ErrorType.Conflict => "Çakışma",
        _ => "Sunucu hatası",
    };
}
