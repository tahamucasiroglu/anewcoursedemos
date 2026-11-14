using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Agents.Application.Models;

namespace TahaKonsept.Modules.Agents.Api.Controllers.Base;

/// <summary>
/// Agents controller tabanı: <see cref="ApiControllerBase"/> + dosya yanıtı yardımcısı (<see cref="ToFileResult"/>; Range destekli).
/// </summary>
public abstract class AgentsControllerBase : ApiControllerBase
{
    /// <summary>Artefakt içeriğini dosya yanıtına çevirir (başarısızlık → ProblemDetails).</summary>
    /// <param name="result">Sonuç.</param>
    /// <returns>HTTP yanıtı.</returns>
    protected IActionResult ToFileResult(Result<ArtifactContent> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess
            ? File(result.Value.Content, result.Value.ContentType, result.Value.FileName, enableRangeProcessing: true)
            : ToProblem(result.Error);
    }
}
