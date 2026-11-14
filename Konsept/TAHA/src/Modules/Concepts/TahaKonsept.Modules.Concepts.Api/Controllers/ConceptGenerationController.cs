using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Api.Controllers;

/// <summary>
/// Konu önerisi ve toplu üretim uçları: <c>POST api/concepts/topics/suggest</c>, <c>POST api/concepts/topics/dislike</c>,
/// <c>POST api/concepts/topics/generate</c>, <c>POST api/concepts/from-idea</c> (arka plan işi, hemen döner),
/// <c>GET api/concepts/jobs/current</c>, <c>POST api/concepts/jobs/current/cancel</c>, <c>POST api/concepts/topic-memory/reset?all=</c>.
/// </summary>
/// <param name="topics">Konu önerisi servisi.</param>
/// <param name="generation">Toplu üretim servisi.</param>
[Route("api/concepts")]
public sealed class ConceptGenerationController(ITopicSuggestionService topics, IConceptGenerationService generation) : ApiControllerBase
{
    /// <summary>Başlık önerir (ucuz model; hafızadakiler elenir).</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="TopicSuggestionsResponse"/>.</returns>
    [HttpPost("topics/suggest")]
    public async Task<IActionResult> Suggest([FromBody] SuggestTopicsRequest request, CancellationToken ct) => ToActionResult(await topics.SuggestAsync(request, ct));

    /// <summary>Beğenilmeyen başlıkları negatif örnek olarak kaydeder.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpPost("topics/dislike")]
    public async Task<IActionResult> Dislike([FromBody] DislikeTopicsRequest request, CancellationToken ct) => ToActionResult(await topics.DislikeAsync(request, ct));

    /// <summary>Seçilen başlıklardan üretimi başlatır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptJobDto"/>.</returns>
    [HttpPost("topics/generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateFromTopicsRequest request, CancellationToken ct) =>
        ToActionResult(await generation.StartFromTopicsAsync(request, ct));

    /// <summary>Fikirden üretimi başlatır.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="ConceptJobDto"/>.</returns>
    [HttpPost("from-idea")]
    public async Task<IActionResult> FromIdea([FromBody] GenerateFromIdeaRequest request, CancellationToken ct) =>
        ToActionResult(await generation.StartFromIdeaAsync(request, ct));

    /// <summary>Güncel üretim işi.</summary>
    /// <returns><see cref="ConceptJobDto"/>.</returns>
    [HttpGet("jobs/current")]
    public IActionResult CurrentJob() => ToActionResult(generation.GetCurrent());

    /// <summary>Çalışan ve sıradaki işleri iptal eder.</summary>
    /// <returns><see cref="ConceptJobDto"/>.</returns>
    [HttpPost("jobs/current/cancel")]
    public IActionResult CancelJob() => ToActionResult(generation.CancelCurrent());

    /// <summary>Konu hafızasını sıfırlar.</summary>
    /// <param name="all">true: üretilen/silinen izleri de sil.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><see cref="TopicMemoryResetResponse"/>.</returns>
    [HttpPost("topic-memory/reset")]
    public async Task<IActionResult> ResetMemory([FromQuery] bool all, CancellationToken ct) => ToActionResult(await topics.ResetMemoryAsync(all, ct));
}
