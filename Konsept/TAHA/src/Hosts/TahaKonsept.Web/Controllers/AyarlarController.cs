using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Ayarlar sayfası ve Ajax uçları. Bölümler: API anahtarları, Okunuş sözlüğü, Masaüstü programı eşleme, Önerileri sıfırla,
/// Tema, Bütçe limiti. Beklenen API rotaları:
/// <c>GET|PUT api/providers/keys</c>, <c>DELETE api/providers/keys/{platform}</c>,
/// <c>GET|POST api/production/pronunciations</c>, <c>DELETE api/production/pronunciations/{id}</c>,
/// <c>POST api/production/pronunciations/preview</c>, <c>POST api/concepts/topic-memory/reset</c>,
/// <c>GET|PUT api/providers/budget</c>, <c>PUT api/identity/me/preferences</c>,
/// <c>GET api/agents</c>, <c>POST api/agents/device/approve</c>, <c>DELETE api/agents/{id}</c>.
/// </summary>
public sealed class AyarlarController : WebControllerBase
{
    /// <summary>Sayfa.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>Tema tercihini kullanıcı ayarına yazar (<c>{ theme: light|dark|system }</c>).</summary>
    /// <param name="body">Tercih.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut]
    public Task<IActionResult> Tema([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PutAsync<JsonElement, JsonElement>("api/identity/me/preferences", body, ct));

    /// <summary>Kayıtlı API anahtarları (yalnızca platform + son 4 hane maskesi).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> ApiAnahtarlari(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/providers/keys", ct));

    /// <summary>API anahtarı ekler / değiştirir (<c>{ platform, apiKey }</c>; sunucuda AES-GCM ile şifrelenir).</summary>
    /// <param name="body">Anahtar.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut]
    public Task<IActionResult> ApiAnahtari([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PutAsync<JsonElement, JsonElement>("api/providers/keys", body, ct));

    /// <summary>API anahtarını siler.</summary>
    /// <param name="id">Platform adı.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> ApiAnahtariSil(string id, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/providers/keys/{Seg(id)}", ct));

    /// <summary>Okunuş sözlüğü (yazılış → okunuş).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Sozluk(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/production/pronunciations", ct));

    /// <summary>Sözlüğe kayıt ekler / günceller (<c>{ id?, written, spoken, profileId? }</c>).</summary>
    /// <param name="body">Kayıt.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    [ActionName("Sozluk")]
    public Task<IActionResult> SozlukKaydet([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/production/pronunciations", body, ct));

    /// <summary>Sözlük kaydını siler.</summary>
    /// <param name="id">Kayıt kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> SozlukSil(string id, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/production/pronunciations/{Seg(id)}", ct));

    /// <summary>Okunuşu dinletir (<c>{ text }</c> → ajan/online TTS önizleme).</summary>
    /// <param name="body">Metin.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> SozlukDinle([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/production/pronunciations/preview", body, ct));

    /// <summary>Konu önerisi hafızasını sıfırlar (daha önce önerilen/silinen konular yeniden önerilebilir).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> OnerileriSifirla(CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>("api/concepts/topic-memory/reset", new { }, ct));

    /// <summary>Aylık bütçe limiti.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Butce(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/providers/budget", ct));

    /// <summary>Aylık bütçe limitini kaydeder (<c>{ monthlyLimitUsd, stopOnExceed }</c>).</summary>
    /// <param name="body">Limit.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut]
    [ActionName("Butce")]
    public Task<IActionResult> ButceKaydet([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PutAsync<JsonElement, JsonElement>("api/providers/budget", body, ct));

    /// <summary>Eşlenmiş masaüstü programları (çevrimiçi durumu, sürüm, GPU).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Ajanlar(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/agents", ct));

    /// <summary>Masaüstü programının gösterdiği kodu onaylar (<c>{ userCode: "XXXX-XXXX" }</c>).</summary>
    /// <param name="body">Kullanıcı kodu.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> CihazOnayla([FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>("api/agents/device/approve", body, ct));

    /// <summary>Masaüstü programının eşlemesini kaldırır (token'ları iptal edilir).</summary>
    /// <param name="id">Ajan kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> AjanKaldir(string id, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/agents/{Seg(id)}", ct));
}
