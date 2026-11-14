using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Base;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Concrete;

/// <summary>
/// Kullanıcı dizini: yasaklamada kullanıcının tüm token'ları (ajan dahil) iptal edilir ve bildirimciler çağrılır;
/// son etkin yönetici yasaklanamaz.
/// </summary>
public sealed class UserDirectory : UserDirectoryBase
{
    private readonly UserManager<AppUser> _users;
    private readonly ITokenRevoker _revoker;
    private readonly IClock _clock;
    private readonly ILogger<UserDirectory> _logger;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="userQueries">Kullanıcı sorguları.</param>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    /// <param name="revoker">Token iptali.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Günlükleyici.</param>
    public UserDirectory(IUserReadRepository userQueries, UserManager<AppUser> users, ITokenRevoker revoker, IClock clock, ILogger<UserDirectory> logger)
        : base(userQueries)
    {
        _users = users;
        _revoker = revoker;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result> SetBannedAsync(Guid userId, bool banned, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (user.IsBanned == banned)
        {
            return Result.Success();
        }

        if (!banned)
        {
            user.Unban();
            await _users.UpdateAsync(user);
            _logger.LogInformation("Kullanıcının engeli kaldırıldı: {UserId}", userId);
            return Result.Success();
        }

        if (await _users.IsInRoleAsync(user, RoleNames.Admin) && await UserQueries.CountActiveInRoleAsync(RoleNames.Admin, ct) <= 1)
        {
            return IdentityErrors.LastAdmin;
        }

        user.Ban(_clock.UtcNow);
        await _users.UpdateAsync(user);
        _logger.LogWarning("Kullanıcı engellendi: {UserId}", userId);
        var revoked = await _revoker.RevokeAllAsync(userId, ct);
        return revoked.IsSuccess ? Result.Success() : revoked.Error;
    }
}
