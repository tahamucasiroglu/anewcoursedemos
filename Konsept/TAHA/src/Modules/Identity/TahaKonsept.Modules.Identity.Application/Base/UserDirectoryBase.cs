using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// <see cref="IUserDirectory"/> tabanı: liste ve sayım ortak (<see cref="IUserReadRepository"/>); yasaklama kuralı türetilen sınıfta.
/// </summary>
public abstract class UserDirectoryBase : IUserDirectory
{
    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="userQueries">Kullanıcı sorguları.</param>
    protected UserDirectoryBase(IUserReadRepository userQueries)
    {
        UserQueries = userQueries ?? throw new ArgumentNullException(nameof(userQueries));
    }

    /// <summary>Kullanıcı sorguları.</summary>
    protected IUserReadRepository UserQueries { get; }

    /// <summary>Tüm kullanıcıları listeler.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Kullanıcı özetleri.</returns>
    public async Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken ct = default)
    {
        var users = await UserQueries.ListAsync(ct);
        var admins = await UserQueries.ListUserIdsInRoleAsync(RoleNames.Admin, ct);
        return users
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                IsAdmin = admins.Contains(u.Id),
                IsBanned = u.IsBanned,
                CreatedAtUtc = u.CreatedAtUtc,
                LastLoginAtUtc = u.LastLoginAtUtc,
            })
            .ToArray();
    }

    /// <summary>Toplam kullanıcı sayısı.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sayı.</returns>
    public Task<int> CountAsync(CancellationToken ct = default) => UserQueries.CountAsync(ct);

    /// <inheritdoc />
    public abstract Task<Result> SetBannedAsync(Guid userId, bool banned, CancellationToken ct = default);
}
