using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Shared.Infrastructure.Http;

/// <summary>
/// <see cref="Result{T}"/> değerini HTTP yanıtına çevirir: başarı 200, beklenen hata ProblemDetails
/// (Türkçe mesaj + kararlı hata kodu; varsa ek bilgi extensions'a, alan hataları <c>errors</c>'a). Controller'ların
/// tek satır kalmasını sağlar (K-4).
/// </summary>
public static class ResultActionResultExtensions
{
    /// <summary>Asenkron sonucu HTTP yanıtına çevirir.</summary>
    /// <typeparam name="T">Başarı değerinin tipi.</typeparam>
    /// <param name="resultTask">Servis çağrısı.</param>
    /// <returns>HTTP yanıtı.</returns>
    public static async Task<IActionResult> ToActionResult<T>(this Task<Result<T>> resultTask)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        var error = result.Error!;
        var status = StatusCodeOf(error.Type);
        var problem = error.FieldErrors is null
            ? new ProblemDetails { Status = status, Title = error.Message }
            : new ValidationProblemDetails(error.FieldErrors.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal))
            {
                Status = status,
                Title = error.Message,
            };
        problem.Extensions["code"] = error.Code;
        if (error.Metadata is not null)
        {
            foreach (var (key, value) in error.Metadata)
            {
                problem.Extensions[key] = value;
            }
        }

        return new ObjectResult(problem) { StatusCode = status };
    }

    /// <summary>Hata türünün HTTP durum kodu.</summary>
    /// <param name="type">Hata türü.</param>
    /// <returns>Durum kodu.</returns>
    public static int StatusCodeOf(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        ErrorType.Timeout => StatusCodes.Status504GatewayTimeout,
        _ => StatusCodes.Status500InternalServerError,
    };
}
