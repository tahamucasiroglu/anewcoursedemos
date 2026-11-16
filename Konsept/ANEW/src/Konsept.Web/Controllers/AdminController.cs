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
}
