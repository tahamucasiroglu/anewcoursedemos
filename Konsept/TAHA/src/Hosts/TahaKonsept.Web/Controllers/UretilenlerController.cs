using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;

namespace TahaKonsept.Web.Controllers;

/// <summary>
/// Üretilen Videolar sayfası ve Ajax uçları. Beklenen API rotaları:
/// <c>GET api/production/videos</c>, <c>GET api/production/videos/{id}</c>, <c>POST api/production/videos/{id}/open-folder</c>,
/// <c>POST api/production/videos/{id}/open-in-player</c>, <c>POST api/production/videos/{id}/new-version</c>,
/// <c>POST api/production/videos/{id}/regenerate</c>, <c>DELETE api/production/videos/{id}</c>.
/// </summary>
public sealed class UretilenlerController : WebControllerBase
{
    /// <summary>Sayfa.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>Video listesi.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Liste(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/production/videos", ct));

    /// <summary>Video ayrıntısı (sahneler, seslendirme, konsept metni, aşama süreleri, ücret, oynatma adresi).</summary>
    /// <param name="id">Video kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Detay(string id, CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>($"api/production/videos/{Seg(id)}", ct));

    /// <summary>Masaüstü programına "klasörde aç" komutu.</summary>
    /// <param name="id">Video kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> KlasordeAc(string id, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/production/videos/{Seg(id)}/open-folder", new { }, ct));

    /// <summary>Masaüstü programına "sistem oynatıcısında aç" komutu.</summary>
    /// <param name="id">Video kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> SistemOynaticisi(string id, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/production/videos/{Seg(id)}/open-in-player", new { }, ct));

    /// <summary>Yeni sürüm: yalnızca değişen aşamaları yeniden çalıştırır (<c>{ settings, document }</c>).</summary>
    /// <param name="id">Video kimliği.</param>
    /// <param name="body">Değişen ayarlar / belge.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> YeniSurum(string id, [FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>($"api/production/videos/{Seg(id)}/new-version", body, ct));

    /// <summary>Baştan yeniden üretir (önce düzenlenmiş konseptle).</summary>
    /// <param name="id">Video kimliği.</param>
    /// <param name="body">Düzenlenmiş belge (isteğe bağlı).</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> YenidenUret(string id, [FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<JsonElement, JsonElement>($"api/production/videos/{Seg(id)}/regenerate", body, ct));

    /// <summary>Videoyu siler (ajan diskteki dosyayı da siler).</summary>
    /// <param name="id">Video kimliği.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpDelete]
    public Task<IActionResult> Sil(string id, CancellationToken ct) =>
        ProxyAsync(c => c.DeleteAsync<JsonElement>($"api/production/videos/{Seg(id)}", ct));
}
