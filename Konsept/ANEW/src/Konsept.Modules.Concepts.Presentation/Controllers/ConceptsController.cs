using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Modules.Concepts.Presentation.Controllers;

/// <summary>
/// Konseptler (spec 0014): üretim durumu, Konu üret, Seçtiklerini üret, fikirden Üret, Durdur; Üretilenler listesi, detay, onay ve
/// onayı geri al. Giriş ister (default-deny); sahiplik servistedir (başkasının verisi 404, BR-1).
/// </summary>
[ApiController]
[Route("api/concepts")]
public sealed class ConceptsController : ControllerBase
{
    private readonly IConceptWorkService _work;
    private readonly IConceptService _concepts;
    private readonly IConceptEditService _edit;

    /// <summary>Initializes a new instance of the <see cref="ConceptsController"/> class.</summary>
    /// <param name="work">Üretim alanları.</param>
    /// <param name="concepts">Üretilenler ve onay.</param>
    /// <param name="edit">Düzenleme döngüsü (spec 0015).</param>
    public ConceptsController(IConceptWorkService work, IConceptService concepts, IConceptEditService edit)
    {
        _work = work;
        _concepts = concepts;
        _edit = edit;
    }

    /// <summary>Profilin üretim durumu (hazır olma, bekleyen öneriler, işler).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpGet("work")]
    public Task<IActionResult> Work([FromQuery] Guid profileId, CancellationToken cancellationToken) =>
        _work.GetAsync(profileId, cancellationToken).ToActionResult();

    /// <summary>Konu üret.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("topics")]
    public Task<IActionResult> Topics([FromBody] StartTopicsRequest request, CancellationToken cancellationToken) =>
        _work.StartTopicsAsync(request, cancellationToken).ToActionResult();

    /// <summary>Seçtiklerini üret.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("from-topics")]
    public Task<IActionResult> FromTopics([FromBody] StartFromTopicsRequest request, CancellationToken cancellationToken) =>
        _work.StartFromTopicsAsync(request, cancellationToken).ToActionResult();

    /// <summary>Fikirden üret.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("from-idea")]
    public Task<IActionResult> FromIdea([FromBody] StartFromIdeaRequest request, CancellationToken cancellationToken) =>
        _work.StartFromIdeaAsync(request, cancellationToken).ToActionResult();

    /// <summary>Durdur.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Durum.</returns>
    [HttpPost("jobs/stop")]
    public Task<IActionResult> Stop([FromBody] StopJobRequest request, CancellationToken cancellationToken) =>
        _work.StopAsync(request, cancellationToken).ToActionResult();

    /// <summary>Üretilenler.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="statuses">Virgüllü durum kodları.</param>
    /// <param name="page">Sayfa.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    [HttpGet]
    public Task<IActionResult> List([FromQuery] Guid profileId, [FromQuery] string? statuses, [FromQuery] int page, CancellationToken cancellationToken) =>
        _concepts.ListAsync(profileId, statuses, page, cancellationToken).ToActionResult();

    /// <summary>Konseptin tamamı.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Detay.</returns>
    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        _concepts.GetAsync(id, cancellationToken).ToActionResult();

    /// <summary>Onayla.</summary>
    /// <param name="request">Kimlikler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değişenler.</returns>
    [HttpPost("approve")]
    public Task<IActionResult> Approve([FromBody] ConceptIdsRequest request, CancellationToken cancellationToken) =>
        _concepts.ApproveAsync(request, cancellationToken).ToActionResult();

    /// <summary>Onayı geri al.</summary>
    /// <param name="request">Kimlikler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Değişenler.</returns>
    [HttpPost("unapprove")]
    public Task<IActionResult> Unapprove([FromBody] ConceptIdsRequest request, CancellationToken cancellationToken) =>
        _concepts.UnapproveAsync(request, cancellationToken).ToActionResult();

    /// <summary>Doğrudan düzenleme: sahneleri kaydet (spec 0015 Req 3).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="request">Sahneler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel konsept.</returns>
    [HttpPost("{id:guid}/scenes")]
    public Task<IActionResult> SaveScenes(Guid id, [FromBody] SaveScenesRequest request, CancellationToken cancellationToken) =>
        _edit.SaveScenesAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Talimatla yeniden üret / Tekrar düzenle (spec 0015 Req 7–8).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="request">Talimat.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Revizyon durumu.</returns>
    [HttpPost("{id:guid}/regenerate")]
    public Task<IActionResult> Regenerate(Guid id, [FromBody] RegenerateRequest request, CancellationToken cancellationToken) =>
        _edit.RegenerateAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Revizyon durumu.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Revizyon durumu.</returns>
    [HttpGet("{id:guid}/revision")]
    public Task<IActionResult> Revision(Guid id, CancellationToken cancellationToken) =>
        _edit.GetRevisionAsync(id, cancellationToken).ToActionResult();

    /// <summary>Revizyonu onayla.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel konsept.</returns>
    [HttpPost("{id:guid}/revision/approve")]
    public Task<IActionResult> ApproveRevision(Guid id, CancellationToken cancellationToken) =>
        _edit.ApproveRevisionAsync(id, cancellationToken).ToActionResult();

    /// <summary>Revizyonu sil.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Revizyon durumu.</returns>
    [HttpPost("{id:guid}/revision/discard")]
    public Task<IActionResult> DiscardRevision(Guid id, CancellationToken cancellationToken) =>
        _edit.DiscardRevisionAsync(id, cancellationToken).ToActionResult();
}
