using Konsept.Modules.Identity.Application.Abstract;
using Konsept.Modules.Identity.Application.Dtos;
using Konsept.Shared.Infrastructure.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Konsept.Modules.Identity.Presentation.Controllers;

/// <summary>
/// Admin Kullanıcılar uçları (spec 0024): liste, ban, ban kaldırma. Yalnızca <c>admin.panel</c> yetkisiyle (yetki her istekte
/// DB'den, spec 0011); yetkisiz istek 403, veri dönmez, değişiklik olmaz. Rate limit yok (yalnızca admin, LAN).
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthPolicies.AdminPanel)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _users;

    /// <summary>Initializes a new instance of the <see cref="AdminUsersController"/> class.</summary>
    /// <param name="users">Admin kullanıcı servisi.</param>
    public AdminUsersController(IAdminUserService users)
    {
        _users = users;
    }

    /// <summary>Kullanıcılar: 25'erli sayfa, ad/e-posta araması, durum filtresi.</summary>
    /// <param name="page">Sayfa.</param>
    /// <param name="search">Arama.</param>
    /// <param name="status">Durum: all | active | banned.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sayfa.</returns>
    [HttpGet]
    public Task<IActionResult> List(int? page, string? search, string? status, CancellationToken cancellationToken) =>
        _users.ListAsync(new AdminUsersRequest(page, search, status), cancellationToken).ToActionResult();

    /// <summary>Kullanıcıyı banlar (idempotent).</summary>
    /// <param name="id">Kullanıcı.</param>
    /// <param name="request">Sebep.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel satır.</returns>
    [HttpPost("{id:guid}/ban")]
    public Task<IActionResult> Ban(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] BanUserRequest? request, CancellationToken cancellationToken) =>
        _users.BanAsync(id, request, cancellationToken).ToActionResult();

    /// <summary>Banı kaldırır (idempotent).</summary>
    /// <param name="id">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncel satır.</returns>
    [HttpPost("{id:guid}/unban")]
    public Task<IActionResult> Unban(Guid id, CancellationToken cancellationToken) =>
        _users.UnbanAsync(id, cancellationToken).ToActionResult();
}
