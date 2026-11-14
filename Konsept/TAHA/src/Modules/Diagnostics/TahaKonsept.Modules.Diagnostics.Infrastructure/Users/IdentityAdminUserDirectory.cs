using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Infrastructure.Users;

/// <summary>
/// Yönetim paneli kullanıcı dizininin Identity adaptörü: Identity modülünün genel <see cref="IUserDirectory"/> servisini
/// <see cref="Application.Abstract.IAdminUserDirectory"/>'e çevirir (liste → <see cref="AdminUserDto"/>, yasakla/kaldır aynen iletilir;
/// Identity son etkin yöneticinin yasaklanmasını reddeder ve yasaklananın token'larını iptal eder).
/// </summary>
public sealed class IdentityAdminUserDirectory : AdminUserDirectoryBase
{
    private readonly IUserDirectory _users;

    /// <summary>Oluşturur.</summary>
    /// <param name="users">Identity kullanıcı dizini.</param>
    public IdentityAdminUserDirectory(IUserDirectory users)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    /// <summary>Kullanıcıları listeler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kullanıcılar.</returns>
    public override async Task<IReadOnlyList<AdminUserDto>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await _users.ListAsync(cancellationToken);
        return users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            UserName = u.UserName,
            IsAdmin = u.IsAdmin,
            IsBanned = u.IsBanned,
            CreatedAt = u.CreatedAtUtc,
            LastLoginAt = u.LastLoginAtUtc,
        }).ToList();
    }

    /// <summary>Yasak durumunu değiştirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="banned">Yasaklı mı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    public override Task<Result> SetBannedAsync(Guid userId, bool banned, CancellationToken cancellationToken) =>
        _users.SetBannedAsync(userId, banned, cancellationToken);
}
