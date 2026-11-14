using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TahaKonsept.Web.Controllers.Base;
using TahaKonsept.Web.Infrastructure.Auth;

namespace TahaKonsept.Web.Areas.Admin.Controllers;

/// <summary>
/// Yönetim paneli (yalnızca <c>admin.access</c> yetkisi). İstatistik kutuları + Chart.js grafikleri, hata bildirimleri,
/// son aktiviteler. Beklenen API rotaları: <c>GET api/diagnostics/admin/stats</c>, <c>GET api/diagnostics/error-reports</c>.
/// </summary>
[Area("Admin")]
[Authorize(Policy = AuthTokenNames.AdminPolicy)]
public sealed class DashboardController : WebControllerBase
{
    /// <summary>Panel sayfası.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>
    /// İstatistikler: <c>{ totalClips, totalConcepts, monthlySpendUsd, errorCount, daily: [{ date, clips, spendUsd }],
    /// byProvider: [{ name, spendUsd }], recentActivities: [...] }</c>.
    /// </summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> Istatistik(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/diagnostics/admin/stats", ct));

    /// <summary>Kullanıcıların gönderdiği hata bildirimleri.</summary>
    /// <param name="ct">İptal belirteci.</param>
    [HttpGet]
    public Task<IActionResult> HataBildirimleri(CancellationToken ct) =>
        ProxyAsync(c => c.GetAsync<JsonElement>("api/diagnostics/error-reports", ct));
}
