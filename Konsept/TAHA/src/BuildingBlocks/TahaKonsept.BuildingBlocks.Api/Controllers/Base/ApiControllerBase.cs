using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Results;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.BuildingBlocks.Api.Controllers.Base;

/// <summary>
/// Tüm API controller'larının tabanı. Varsayılan olarak <c>[Authorize]</c> (anonim uçlar <c>[AllowAnonymous]</c> ile açılır).
/// Aksiyonlar tek satırdır: <c>=&gt; ToActionResult(await _service.XAsync(dto, ct));</c>
/// Önemli metotlar: <see cref="ToActionResult(Result)"/> (başarı → 204), <see cref="ToActionResult{T}(Result{T})"/> (başarı → 200 + değer),
/// <see cref="ToCreatedResult{T}(Result{T})"/> (başarı → 201). Başarısızlık → ProblemDetails (<see cref="ProblemDetailsMapper"/>).
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Değersiz sonucu HTTP yanıtına çevirir (başarı: 204 No Content).</summary>
    /// <param name="result">Sonuç.</param>
    /// <returns>HTTP yanıtı.</returns>
    protected IActionResult ToActionResult(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? NoContent() : ToProblem(result.Error);
    }

    /// <summary>Değerli sonucu HTTP yanıtına çevirir (başarı: 200 OK + değer).</summary>
    /// <typeparam name="T">Değer türü.</typeparam>
    /// <param name="result">Sonuç.</param>
    /// <returns>HTTP yanıtı.</returns>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? Ok(result.Value) : ToProblem(result.Error);
    }

    /// <summary>Oluşturma sonucunu HTTP yanıtına çevirir (başarı: 201 Created + değer).</summary>
    /// <typeparam name="T">Değer türü.</typeparam>
    /// <param name="result">Sonuç.</param>
    /// <returns>HTTP yanıtı.</returns>
    protected IActionResult ToCreatedResult<T>(Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : ToProblem(result.Error);
    }

    /// <summary>Hatayı ProblemDetails yanıtına çevirir.</summary>
    /// <param name="error">Hata.</param>
    /// <returns>HTTP yanıtı.</returns>
    protected IActionResult ToProblem(Error error) => ProblemDetailsMapper.ToObjectResult(HttpContext, error);
}
