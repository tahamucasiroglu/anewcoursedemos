using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Api.Controllers;

/// <summary>
/// "Şunları düzelterek tekrar üret" döngüsü: <c>POST api/concepts/{id}/revise</c> (aday üretir, kaydetmez),
/// <c>POST api/concepts/{id}/candidates/{candidateId}/accept</c> (Seç), <c>DELETE api/concepts/{id}/candidates/{candidateId}</c> (Sil).
/// </summary>
/// <param name="service">Düzeltme servisi.</param>
[Route("api/concepts/{id:guid}")]
public sealed class ConceptRevisionsController(IConceptRevisionService service) : ApiControllerBase
{
    /// <summary>Düzeltme adayı üretir.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptRevisionCandidateDto"/>.</returns>
    [HttpPost("revise")]
    public async Task<IActionResult> Revise(Guid id, [FromBody] ReviseConceptRequest request, CancellationToken ct) =>
        ToActionResult(await service.ReviseAsync(id, request, ct));

    /// <summary>Adayı kabul eder.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="candidateId">Aday.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptDetailDto"/>.</returns>
    [HttpPost("candidates/{candidateId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, Guid candidateId, CancellationToken ct) =>
        ToActionResult(await service.AcceptAsync(id, candidateId, ct));

    /// <summary>Adayı atar.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="candidateId">Aday.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpDelete("candidates/{candidateId:guid}")]
    public async Task<IActionResult> Discard(Guid id, Guid candidateId, CancellationToken ct) =>
        ToActionResult(await service.DiscardAsync(id, candidateId, ct));
}
