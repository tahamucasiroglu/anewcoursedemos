using Microsoft.AspNetCore.Mvc;
using TahaKonsept.BuildingBlocks.Api.Controllers.Base;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;

namespace TahaKonsept.Modules.Diagnostics.Api.Controllers;

/// <summary>
/// Yönetim paneli uçları (yetki kontrolü serviste: <c>admin.access</c>): <c>GET api/diagnostics/admin/stats</c>,
/// <c>GET api/diagnostics/admin/users</c>, <c>POST api/diagnostics/admin/users/{id}/ban</c>, <c>POST api/diagnostics/admin/users/{id}/unban</c>.
/// </summary>
[Route("api/diagnostics/admin")]
public sealed class DiagnosticsAdminController : ApiControllerBase
{
    private readonly IAdminStatsService _stats;
    private readonly IAdminUserService _users;

    /// <summary>Oluşturur.</summary>
    /// <param name="stats">İstatistik servisi.</param>
    /// <param name="users">Kullanıcı servisi.</param>
    public DiagnosticsAdminController(IAdminStatsService stats, IAdminUserService users)
    {
        _stats = stats;
        _users = users;
    }

    /// <summary>İstatistikler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>AdminStatsResponse</c>.</returns>
    [HttpGet("stats")]
    public async Task<IActionResult> Stats(CancellationToken ct) => ToActionResult(await _stats.GetAsync(ct));

    /// <summary>Kullanıcılar.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>AdminUserDto[]</c>.</returns>
    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct) => ToActionResult(await _users.ListAsync(ct));

    /// <summary>Kullanıcıyı yasaklar.</summary>
    /// <param name="id">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpPost("users/{id:guid}/ban")]
    public async Task<IActionResult> Ban(Guid id, CancellationToken ct) => ToActionResult(await _users.BanAsync(id, ct));

    /// <summary>Yasağı kaldırır.</summary>
    /// <param name="id">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>204.</returns>
    [HttpPost("users/{id:guid}/unban")]
    public async Task<IActionResult> Unban(Guid id, CancellationToken ct) => ToActionResult(await _users.UnbanAsync(id, ct));
}
