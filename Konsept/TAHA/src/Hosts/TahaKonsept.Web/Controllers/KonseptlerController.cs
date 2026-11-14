using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Konseptler sayfası ve Ajax uçları (hepsi API'ye tek satır vekil). Beklenen API rotaları:
/// <c>GET api/providers/models?capability=Text</c>, <c>GET|PUT api/concepts/settings</c>,
/// <c>POST api/concepts/topics/suggest</c>, <c>POST api/concepts/topics/generate</c>, <c>POST api/concepts/from-idea</c>,
/// <c>GET api/concepts?status=Generated</c>, <c>GET|PUT|DELETE api/concepts/{id}</c>, <c>POST api/concepts/{id}/revise</c>,
/// <c>POST api/concepts/{id}/approve</c>.
/// </summary>
public sealed class KonseptlerController : WebControllerBase
{
    /// <summary>Sayfa.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>Metin modelleri kataloğu.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Modeller(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/providers/models?capability=Text", ct));

    /// <summary>Konsept üretim ayarları (modeller, efor, ton, izleyici, anlatım, kelime denetimi).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Ayarlar(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/concepts/settings", ct));

    /// <summary>Konsept üretim ayarlarını kaydeder.</summary>
    /// <param name="body">Ayarlar.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut]
    [ActionName("Ayarlar")]
    public Task<IActionResult> AyarlarKaydet([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PutAsync<JsonElement, JsonElement>("api/concepts/settings", body, ct));

    /// <summary>Başlık önerir (<c>{ count, hint }</c>).</summary>
    /// <param name="body">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> KonuOner([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/concepts/topics/suggest", body, ct));

    /// <summary>Seçilen başlıkları detaylı konsepte çevirir (<c>{ topics: [{ id, title }] }</c>).</summary>
    /// <param name="body">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> SecilenleriUret([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/concepts/topics/generate", body, ct));

    /// <summary>Serbest fikirden konsept üretir (<c>{ idea, count }</c>).</summary>
    /// <param name="body">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> FikirdenUret([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/concepts/from-idea", body, ct));

    /// <summary>Üretilen (onaylanmamış) konseptler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Liste(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/concepts?status=Generated", ct));

    /// <summary>Tek konsept (belgesiyle).</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Getir(string id, CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>($"api/concepts/{Seg(id)}", ct));

    /// <summary>Konsepti günceller (<c>{ title, document }</c>).</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="body">Güncel belge.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut]
    public Task<IActionResult> Guncelle(string id, [FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PutAsync<JsonElement, JsonElement>($"api/concepts/{Seg(id)}", body, ct));

    /// <summary>Konsepti Silinenler'e taşır.</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> Sil(string id, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/concepts/{Seg(id)}", ct));

    /// <summary>"Şunları düzelterek tekrar üret" (<c>{ instruction, document }</c>) → düzeltilmiş belge.</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="body">Talimat.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Revize(string id, [FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>($"api/concepts/{Seg(id)}/revise", body, ct));

    /// <summary>Konsepti onaylar (Konseptten Video listesine geçer).</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Onayla(string id, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/concepts/{Seg(id)}/approve", new { }, ct));
}
