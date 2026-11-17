using System.Text.Json;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Konseptler sayfasının üretim ve Üretilenler Ajax uçları (spec 0014): üretim durumu, Konu üret, Seçtiklerini üret, fikirden Üret,
/// Durdur, liste, detay, onayla, onayı geri al; düzenleme (0015); silme, Silinenler, kalıcı silme ve önerileri sıfırla (0016). Giriş ister (default-deny), POST CSRF korumalıdır; kurallar ve sahiplik Api'dedir
/// (Web yalnızca iletir, K-6).
/// </summary>
public sealed class ConceptsController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="ConceptsController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public ConceptsController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Profilin üretim durumu (Ajax, 2 sn yoklama).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/konsept-uretim/{profileId:guid}")]
    public Task<IActionResult> Work(Guid profileId, CancellationToken cancellationToken) =>
        _api.GetConceptWorkAsync(profileId, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Konu üret.</summary>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konsept-uretim/konu")]
    public Task<IActionResult> Topics([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptCommandAsync(ConceptCommand.Topics, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Seçtiklerini üret.</summary>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konsept-uretim/secilenler")]
    public Task<IActionResult> FromTopics([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptCommandAsync(ConceptCommand.FromTopics, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Fikirden üret.</summary>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konsept-uretim/fikir")]
    public Task<IActionResult> FromIdea([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptCommandAsync(ConceptCommand.FromIdea, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Durdur.</summary>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konsept-uretim/durdur")]
    public Task<IActionResult> Stop([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptCommandAsync(ConceptCommand.Stop, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Üretilenler.</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="durumlar">Virgüllü durum kodları.</param>
    /// <param name="sayfa">Sayfa.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/konseptler/liste/{profileId:guid}")]
    public Task<IActionResult> List(Guid profileId, [FromQuery] string? durumlar, [FromQuery] int sayfa, CancellationToken cancellationToken) =>
        _api.ListConceptsAsync(profileId, durumlar, sayfa, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Konseptin tamamı.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/konseptler/detay/{id:guid}")]
    public Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken) =>
        _api.GetConceptAsync(id, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Onayla.</summary>
    /// <param name="body">Gövde (<c>ids</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/onayla")]
    public Task<IActionResult> Approve([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptCommandAsync(ConceptCommand.Approve, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Onayı geri al.</summary>
    /// <param name="body">Gövde (<c>ids</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/onayi-geri-al")]
    public Task<IActionResult> Unapprove([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptCommandAsync(ConceptCommand.Unapprove, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Doğrudan düzenleme: sahneleri kaydet (spec 0015).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="body">Gövde (<c>scenes</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/{id:guid}/sahneler")]
    public Task<IActionResult> SaveScenes(Guid id, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptEditAsync(id, ConceptEditCommand.SaveScenes, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Talimatla yeniden üret / Tekrar düzenle (spec 0015).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="body">Gövde (<c>instruction</c>, isteğe bağlı <c>scenes</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/{id:guid}/yeniden-uret")]
    public Task<IActionResult> Regenerate(Guid id, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptEditAsync(id, ConceptEditCommand.Regenerate, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Revizyon durumu (Ajax yoklama).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/konseptler/{id:guid}/revizyon")]
    public Task<IActionResult> Revision(Guid id, CancellationToken cancellationToken) =>
        _api.GetConceptRevisionAsync(id, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Revizyonu onayla.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/{id:guid}/revizyon/onayla")]
    public Task<IActionResult> ApproveRevision(Guid id, CancellationToken cancellationToken) =>
        _api.SendConceptEditAsync(id, ConceptEditCommand.ApproveRevision, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeniden üret bağlamında sahneleri kaydet (spec 0020 Req 15; belirteç sorguda, Video oldu konsept).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="body">Gövde (<c>scenes</c>).</param>
    /// <param name="regen">Bağlam belirteci.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/{id:guid}/baglam/sahneler")]
    public Task<IActionResult> SaveScenesInContext(Guid id, [FromBody] JsonElement? body, [FromQuery] string? regen, CancellationToken cancellationToken) =>
        _api.SendConceptEditInContextAsync(id, ConceptEditCommand.SaveScenes, body, regen ?? string.Empty, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeniden üret bağlamında talimatla düzenlet (spec 0020 Req 15).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="body">Gövde (<c>instruction</c>, isteğe bağlı <c>scenes</c>).</param>
    /// <param name="regen">Bağlam belirteci.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/{id:guid}/baglam/yeniden-uret")]
    public Task<IActionResult> RegenerateInContext(Guid id, [FromBody] JsonElement? body, [FromQuery] string? regen, CancellationToken cancellationToken) =>
        _api.SendConceptEditInContextAsync(id, ConceptEditCommand.Regenerate, body, regen ?? string.Empty, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Yeniden üret bağlamında revizyonu onayla (spec 0020 Req 15).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="regen">Bağlam belirteci.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/{id:guid}/baglam/revizyon/onayla")]
    public Task<IActionResult> ApproveRevisionInContext(Guid id, [FromQuery] string? regen, CancellationToken cancellationToken) =>
        _api.SendConceptEditInContextAsync(id, ConceptEditCommand.ApproveRevision, null, regen ?? string.Empty, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Revizyonu sil.</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/{id:guid}/revizyon/sil")]
    public Task<IActionResult> DiscardRevision(Guid id, CancellationToken cancellationToken) =>
        _api.SendConceptEditAsync(id, ConceptEditCommand.DiscardRevision, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Konsepti Silinenler'e taşı (spec 0016).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/konseptler/{id:guid}/sil")]
    public Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        _api.SendConceptEditAsync(id, ConceptEditCommand.Delete, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Silinenler: profilin silinmiş konseptleri (spec 0016).</summary>
    /// <param name="profileId">Profil.</param>
    /// <param name="sayfa">Sayfa.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/silinenler/konseptler/{profileId:guid}")]
    public Task<IActionResult> Deleted(Guid profileId, [FromQuery] int sayfa, CancellationToken cancellationToken) =>
        _api.ListDeletedConceptsAsync(profileId, sayfa, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Silinmiş konseptin tamamı (salt okunur).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/silinenler/konseptler/detay/{id:guid}")]
    public Task<IActionResult> DeletedDetail(Guid id, CancellationToken cancellationToken) =>
        _api.GetDeletedConceptAsync(id, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Kalıcı sil (spec 0016).</summary>
    /// <param name="id">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/silinenler/konseptler/{id:guid}/kalici-sil")]
    public Task<IActionResult> Purge(Guid id, CancellationToken cancellationToken) =>
        _api.SendConceptEditAsync(id, ConceptEditCommand.Purge, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Önerileri sıfırla (spec 0016).</summary>
    /// <param name="body">Gövde (<c>profileId</c>).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/oneriler/sifirla")]
    public Task<IActionResult> ResetSuggestions([FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SendConceptCommandAsync(ConceptCommand.ResetSuggestions, body, cancellationToken).ToDetailedBrowserJson(HttpContext);
}
