using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Kanal profilleri için Ajax uçları (üst bardaki dropdown, ✏️ ve ➕ pencereleri; ayrı sayfa yok). Beklenen API rotaları:
/// <c>GET api/profiles</c>, <c>GET api/profiles/templates</c>, <c>GET api/profiles/{id}</c>, <c>POST api/profiles</c>,
/// <c>PUT api/profiles/{id}</c>, <c>POST api/profiles/{id}/activate</c>, <c>DELETE api/profiles/{id}</c>.
/// Gövde alanları (camelCase): <c>{ name, language, format: Vertical|Horizontal, targetDurationSec, channelDescription,
/// defaultTone?, defaultAudience?, defaultNarrativeStyle?, templateKey? }</c> (Shared.Contracts <c>SaveProfileRequest</c>).
/// </summary>
public sealed class ProfilController : WebControllerBase
{
    /// <summary>Profiller (<c>ProfileDto[]</c>: <c>{ id, name, language, format, targetDurationSec, channelDescription, isActive, … }</c>).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Liste(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/profiles", ct));

    /// <summary>Hazır şablonlar (<c>ProfileTemplateDto[]</c>: <c>{ key, category, title, name, language, format, targetDurationSec, channelDescription, … }</c>).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Sablonlar(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/profiles/templates", ct));

    /// <summary>Tek profil.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Getir(string id, CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>($"api/profiles/{Seg(id)}", ct));

    /// <summary>Yeni profil (oluşan profil aktif olur).</summary>
    /// <param name="body">Profil.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Ekle([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/profiles", body, ct));

    /// <summary>Profili günceller.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="body">Profil.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut]
    public Task<IActionResult> Guncelle(string id, [FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PutAsync<JsonElement, JsonElement>($"api/profiles/{Seg(id)}", body, ct));

    /// <summary>Aktif profili seçer.</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Sec(string id, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/profiles/{Seg(id)}/activate", new { }, ct));

    /// <summary>Profili siler (aktifse en yeni kalan profil aktif olur).</summary>
    /// <param name="id">Profil kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> Sil(string id, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/profiles/{Seg(id)}", ct));
}
