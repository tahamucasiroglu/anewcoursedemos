using System.Text.Json;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Konseptler sayfasının üretim ve Üretilenler Ajax uçları (spec 0014): üretim durumu, Konu üret, Seçtiklerini üret, fikirden Üret,
/// Durdur, liste, detay, onayla, onayı geri al. Giriş ister (default-deny), POST CSRF korumalıdır; kurallar ve sahiplik Api'dedir
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
}
