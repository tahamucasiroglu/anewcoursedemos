using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;
using TahaKonsept.Web.Infrastructure.Auth;

namespace TahaKonsept.Web.Areas.Admin.Controllers;

/// <summary>
/// Yönetim paneli (yalnızca <c>admin.access</c> yetkisi): genel bakış (istatistik kutuları + Chart.js), kullanıcılar (yasakla),
/// hata bildirimleri (durum değiştir) ve etkinlik günlüğü (filtreli). API rotaları (Diagnostics modülü):
/// <c>GET api/diagnostics/admin/stats</c>, <c>GET api/diagnostics/admin/users</c>, <c>POST api/diagnostics/admin/users/{id}/ban|unban</c>,
/// <c>GET api/diagnostics/error-reports?status=&amp;page=&amp;pageSize=</c>, <c>PUT api/diagnostics/error-reports/{id}/status</c>,
/// <c>GET api/diagnostics/activity-logs?day=&amp;level=&amp;source=&amp;search=&amp;page=&amp;pageSize=</c>.
/// </summary>
[Area("Admin")]
[Authorize(Policy = AuthTokenNames.AdminPolicy)]
public sealed class DashboardController : WebControllerBase
{
    /// <summary>Panel sayfası.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>İstatistikler (<c>AdminStatsResponse</c>).</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Istatistik(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/diagnostics/admin/stats", ct));

    /// <summary>Hata bildirimleri (sayfalı, en yeni önce).</summary>
    /// <param name="status">Durum filtresi (<c>New</c>, <c>Seen</c>, <c>Resolved</c>).</param>
    /// <param name="page">Sayfa.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> HataBildirimleri(string? status, int? page, CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>(ApiUrl("api/diagnostics/error-reports", ("status", status), ("page", page), ("pageSize", 20)), ct));

    /// <summary>Hata bildiriminin durumunu değiştirir (<c>{ status }</c>).</summary>
    /// <param name="id">Bildirim.</param>
    /// <param name="body">Yeni durum.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPut]
    public Task<IActionResult> HataDurumu(Guid id, [FromBody] JsonElement body, CancellationToken ct) =>
        ProxyAsync(c => c.PutAsync<JsonElement, JsonElement>($"api/diagnostics/error-reports/{id}/status", body, ct));

    /// <summary>Kullanıcı listesi.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Kullanicilar(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/diagnostics/admin/users", ct));

    /// <summary>Kullanıcıyı yasaklar.</summary>
    /// <param name="id">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> Yasakla(Guid id, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/diagnostics/admin/users/{id}/ban", new { }, ct));

    /// <summary>Kullanıcının yasağını kaldırır.</summary>
    /// <param name="id">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpPost]
    public Task<IActionResult> YasagiKaldir(Guid id, CancellationToken ct) =>
        ProxyAsync(c => c.PostAsync<object, JsonElement>($"api/diagnostics/admin/users/{id}/unban", new { }, ct));

    /// <summary>Etkinlik günlüğü (filtreli, sayfalı, en yeni önce).</summary>
    /// <param name="day">Gün (<c>yyyy-MM-dd</c>, Türkiye saati).</param>
    /// <param name="level">Seviye.</param>
    /// <param name="source">Kaynak.</param>
    /// <param name="search">Mesajda aranan metin.</param>
    /// <param name="page">Sayfa.</param>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Etkinlikler(string? day, string? level, string? source, string? search, int? page, CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>(
            ApiUrl("api/diagnostics/activity-logs", ("day", day), ("level", level), ("source", source), ("search", search), ("page", page), ("pageSize", 50)),
            ct));
}
