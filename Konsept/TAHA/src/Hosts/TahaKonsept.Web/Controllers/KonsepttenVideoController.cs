using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Konseptten Video sayfası ve Ajax uçları. Beklenen API rotaları:
/// <c>GET api/providers/models</c>, <c>GET|PUT api/production/settings</c>, <c>GET api/concepts?status=Approved</c>,
/// <c>GET api/production/queue</c>, <c>POST api/production/queue</c>, <c>POST api/production/queue/{id}/move</c>,
/// <c>DELETE api/production/queue/{id}</c>, <c>POST api/production/queue/{start|pause|resume}</c>,
/// <c>GET api/production/videos?take=50</c>.
/// </summary>
public sealed class KonsepttenVideoController : WebControllerBase
{
    /// <summary>Sayfa.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>Tüm model kataloğu (metin, görsel, ses, video motoru; VRAM etiketli).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Modeller(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/providers/models", ct));

    /// <summary>Üretim ayarları (video kodu modeli + efor, motor, görsel, ses, altyazı, ilerleme çubuğu).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Ayarlar(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/production/settings", ct));

    /// <summary>Üretim ayarlarını kaydeder.</summary>
    /// <param name="body">Ayarlar.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut]
    [ActionName("Ayarlar")]
    public Task<IActionResult> AyarlarKaydet([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PutAsync<JsonElement, JsonElement>("api/production/settings", body, ct));

    /// <summary>Onaylanan konseptler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Onaylananlar(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/concepts?status=Approved", ct));

    /// <summary>Üretim sırası ve durumu (aşama aşama ilerleme; 2 sn polling).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Sira(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/production/queue", ct));

    /// <summary>Konseptleri sıraya ekler (<c>{ conceptIds, settings }</c>).</summary>
    /// <param name="body">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> SirayaEkle([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/production/queue", body, ct));

    /// <summary>Sıradaki işi taşır (<c>{ direction: -1 | 1 }</c>).</summary>
    /// <param name="id">İş kimliği.</param>
    /// <param name="body">Yön.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Tasi(string id, [FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>($"api/production/queue/{Seg(id)}/move", body, ct));

    /// <summary>İşi sıradan çıkarır (çalışıyorsa iptal eder).</summary>
    /// <param name="id">İş kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> SiradanCikar(string id, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/production/queue/{Seg(id)}", ct));

    /// <summary>Sırayı baştan sona üretmeye başlar.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Baslat(CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>("api/production/queue/start", new { }, ct));

    /// <summary>Üretimi durdurur (aşama sonunda).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Durdur(CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>("api/production/queue/pause", new { }, ct));

    /// <summary>Kaldığı yerden devam eder.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Devam(CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>("api/production/queue/resume", new { }, ct));

    /// <summary>Son üretilen videolar.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Uretilenler(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/production/videos?take=50", ct));
}
