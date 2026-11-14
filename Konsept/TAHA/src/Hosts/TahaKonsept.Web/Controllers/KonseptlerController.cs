using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Konseptler sayfası ve Ajax uçları (hepsi API'ye tek satır vekil). API rotaları (Concepts modülü):
/// <c>GET api/providers/models?capability=Text</c>, <c>GET|PUT api/concepts/settings</c>,
/// <c>POST api/concepts/topics/suggest</c>, <c>POST api/concepts/topics/dislike</c>, <c>POST api/concepts/topics/generate</c>,
/// <c>POST api/concepts/from-idea</c>, <c>GET api/concepts/jobs/current</c>, <c>POST api/concepts/jobs/current/cancel</c>,
/// <c>GET api/concepts?status=Generated</c>, <c>GET|PUT|DELETE api/concepts/{id}</c>, <c>POST api/concepts/{id}/revise</c>,
/// <c>POST api/concepts/{id}/candidates/{candidateId}/accept</c>, <c>DELETE api/concepts/{id}/candidates/{candidateId}</c>,
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

    /// <summary>Konsept üretim ayarları (modeller, efor, ince ayar, kelime denetimi, gerçek bilgi).</summary>
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

    /// <summary>Beğenilmeyen başlıkları kaydeder (<c>{ titles: [] }</c>); bir daha önerilmez.</summary>
    /// <param name="body">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> KonuBegenme([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/concepts/topics/dislike", body, ct));

    /// <summary>Seçilen başlıklardan arka planda konsept üretimini başlatır (<c>{ titles: [] }</c>) → iş durumu.</summary>
    /// <param name="body">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> SecilenleriUret([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/concepts/topics/generate", body, ct));

    /// <summary>Serbest fikirden arka planda konsept üretimini başlatır (<c>{ idea, count }</c>) → iş durumu.</summary>
    /// <param name="body">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> FikirdenUret([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/concepts/from-idea", body, ct));

    /// <summary>Güncel üretim işinin durumu (ilerleme çubuğu, 2 sn'de bir sorgulanır).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Gorev(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/concepts/jobs/current", ct));

    /// <summary>Çalışan ve sıradaki üretim işlerini iptal eder.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> GorevIptal(CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>("api/concepts/jobs/current/cancel", new { }, ct));

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

    /// <summary>Konsepti doğrudan düzenler (<c>{ title, document }</c>).</summary>
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

    /// <summary>"Şunları düzelterek tekrar üret" (<c>{ instruction, document }</c>) → kaydedilmemiş aday (<c>{ candidateId, document, costUsd }</c>).</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="body">Talimat ve temel belge.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Revize(string id, [FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>($"api/concepts/{Seg(id)}/revise", body, ct));

    /// <summary>Düzeltme adayını seçer (konseptin yerine geçer) → güncel konsept.</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="candidateId">Aday kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> AdayKabul(string id, string candidateId, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/concepts/{Seg(id)}/candidates/{Seg(candidateId)}/accept", new { }, ct));

    /// <summary>Düzeltme adayını atar.</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="candidateId">Aday kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> AdaySil(string id, string candidateId, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/concepts/{Seg(id)}/candidates/{Seg(candidateId)}", ct));

    /// <summary>Konsepti onaylar (Konseptten Video listesine geçer).</summary>
    /// <param name="id">Konsept kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Onayla(string id, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/concepts/{Seg(id)}/approve", new { }, ct));
}
