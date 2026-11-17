using System.Text.Json;
using Konsept.Web.ApiClient.Abstract;
using Konsept.Web.Auth;
using Konsept.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Konsept.Web.Controllers;

/// <summary>
/// Admin paneli (spec 0011 Req 4, spec 0010): yalnızca <c>admin.panel</c> yetkisi olanlara açık sayfa ve Ajax uçları.
/// Yetkisiz sayfa isteği "/yetkisiz"e, yetkisiz Ajax 403 JSON'a gider; asıl karar Api'dedir (özet Api'den istenir).
/// </summary>
[Authorize(Policy = WebPolicies.AdminPanel)]
public sealed class AdminController : Controller
{
    private readonly IKonseptApiClient _api;

    /// <summary>Initializes a new instance of the <see cref="AdminController"/> class.</summary>
    /// <param name="api">API istemcisi.</param>
    public AdminController(IKonseptApiClient api)
    {
        _api = api;
    }

    /// <summary>Admin paneli sayfası (İstatistikler sekmesi seçili açılır).</summary>
    /// <returns>Sayfa.</returns>
    [HttpGet("/admin")]
    public IActionResult Index() => View();

    /// <summary>Admin özeti (Ajax; Api'den).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/admin/ozet")]
    public Task<IActionResult> Ozet(CancellationToken cancellationToken) =>
        _api.GetAdminOverviewAsync(cancellationToken).ToBrowserJson(HttpContext);

    /// <summary>İstatistikler (Ajax; spec 0010): özet sayılar ve günlük seri; hatada korelasyon numarası.</summary>
    /// <param name="gun">Aralık: 7 / 30 / 90.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/admin/istatistikler")]
    public Task<IActionResult> Istatistikler(int? gun, CancellationToken cancellationToken) =>
        _api.GetAdminStatisticsAsync(gun, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Aktiviteler (Ajax; spec 0010): 25'erli sayfa, kullanıcı ve tarih filtresi; hatada korelasyon numarası.</summary>
    /// <param name="sayfa">Sayfa.</param>
    /// <param name="kullanici">Ad/e-posta araması.</param>
    /// <param name="baslangic">Başlangıç günü.</param>
    /// <param name="bitis">Bitiş günü.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/admin/aktiviteler")]
    public Task<IActionResult> Aktiviteler(int sayfa, string? kullanici, string? baslangic, string? bitis, CancellationToken cancellationToken) =>
        _api.GetAdminActivitiesAsync(sayfa, kullanici, baslangic, bitis, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Kullanıcılar (Ajax; spec 0024): 25'erli sayfa, ad/e-posta araması, durum filtresi; hatada korelasyon numarası.</summary>
    /// <param name="sayfa">Sayfa.</param>
    /// <param name="ara">Ad/e-posta araması.</param>
    /// <param name="durum">Durum: all | active | banned.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/admin/kullanicilar")]
    public Task<IActionResult> Kullanicilar(int sayfa, string? ara, string? durum, CancellationToken cancellationToken) =>
        _api.GetAdminUsersAsync(sayfa, ara, durum, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Kullanıcıyı banla (Ajax, antiforgery; spec 0024): gövde <c>{ reason }</c>.</summary>
    /// <param name="id">Kullanıcı.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/admin/kullanicilar/{id:guid}/banla")]
    public Task<IActionResult> Banla(Guid id, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SetUserBanAsync(id, ban: true, body, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Banı kaldır (Ajax, antiforgery; spec 0024).</summary>
    /// <param name="id">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/admin/kullanicilar/{id:guid}/ban-kaldir")]
    public Task<IActionResult> BanKaldir(Guid id, CancellationToken cancellationToken) =>
        _api.SetUserBanAsync(id, ban: false, null, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Hata bildirimleri (Ajax; spec 0025): 25'erli sayfa, kaynak ve durum filtresi; hatada korelasyon numarası.</summary>
    /// <param name="sayfa">Sayfa.</param>
    /// <param name="kaynak">Kaynak: all | web | agent | api.</param>
    /// <param name="durum">Durum: open | new | reviewing | resolved | ignored | all.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/admin/hata-bildirimleri")]
    public Task<IActionResult> HataBildirimleri(int sayfa, string? kaynak, string? durum, CancellationToken cancellationToken) =>
        _api.GetAdminErrorReportsAsync(sayfa, kaynak, durum, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Hata bildirimi detayı (Ajax; spec 0025).</summary>
    /// <param name="numara">Bildirim numarası.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpGet("/admin/hata-bildirimleri/{numara:long}")]
    public Task<IActionResult> HataBildirimi(long numara, CancellationToken cancellationToken) =>
        _api.GetAdminErrorReportAsync(numara, cancellationToken).ToDetailedBrowserJson(HttpContext);

    /// <summary>Hata bildirimi durumunu değiştir (Ajax, antiforgery; spec 0025): gövde <c>{ status, note }</c>.</summary>
    /// <param name="numara">Bildirim numarası.</param>
    /// <param name="body">Gövde.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>JSON.</returns>
    [HttpPost("/admin/hata-bildirimleri/{numara:long}/durum")]
    public Task<IActionResult> HataBildirimiDurum(long numara, [FromBody] JsonElement? body, CancellationToken cancellationToken) =>
        _api.SetErrorReportStatusAsync(numara, body, cancellationToken).ToDetailedBrowserJson(HttpContext);
}
