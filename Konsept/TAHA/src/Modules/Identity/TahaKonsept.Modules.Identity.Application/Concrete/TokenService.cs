using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Base;
using TahaKonsept.Modules.Identity.Application.Errors;
using TahaKonsept.Modules.Identity.Application.Mapping;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Domain.Enums;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Concrete;

/// <summary>
/// Token verme, rotasyon ve iptal. <see cref="IssueAsync"/>: yeni aile; <see cref="RotateAsync"/>: aynı aile içinde
/// tek kullanımlık rotasyon + tekrar kullanım tespitinde aile iptali; <see cref="RevokeFamilyAsync"/>;
/// <see cref="RevokeAllAsync"/>: web + ajan tüm token'lar ve bildirimciler.
/// </summary>
public sealed class TokenService : TokenServiceBase
{
    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    /// <param name="refreshTokens">Refresh token deposu.</param>
    /// <param name="unitOfWork">İş birimi.</param>
    /// <param name="accessTokens">JWT üretici.</param>
    /// <param name="generator">Refresh token üretici.</param>
    /// <param name="permissions">İzin çözücü.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="jwtOptions">JWT ayarları.</param>
    /// <param name="notifiers">Oturum iptal bildirimcileri.</param>
    /// <param name="logger">Günlükleyici.</param>
    public TokenService(
        UserManager<AppUser> users,
        IRefreshTokenRepository refreshTokens,
        IIdentityUnitOfWork unitOfWork,
        IAccessTokenFactory accessTokens,
        IRefreshTokenGenerator generator,
        IPermissionResolver permissions,
        IClock clock,
        JwtOptions jwtOptions,
        IEnumerable<ISessionRevocationNotifier> notifiers,
        ILogger<TokenService> logger)
        : base(users, refreshTokens, unitOfWork, accessTokens, generator, permissions, clock, jwtOptions, notifiers, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<Result<IssuedTokens>> IssueAsync(Guid userId, TokenClientKind kind, CancellationToken ct = default)
    {
        var user = await Users.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return IdentityErrors.UserNotFound;
        }

        if (user.IsBanned)
        {
            return IdentityErrors.UserBanned;
        }

        var familyId = Guid.CreateVersion7();
        var (_, response) = await CreatePairAsync(user, familyId, IdentityEnumMapper.ToDomain(kind), ct);
        await UnitOfWork.SaveChangesAsync(ct);
        Logger.LogInformation("Token ailesi oluşturuldu: kullanıcı {UserId}, tür {Kind}, aile {FamilyId}", user.Id, kind, familyId);
        return new IssuedTokens(response, familyId);
    }

    /// <inheritdoc />
    public override async Task<Result<TokenResponse>> RotateAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var current = await RefreshTokens.FindByHashAsync(Generator.Hash(refreshToken), ct);
        if (current is null)
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        if (current.IsRevoked)
        {
            // Kullanılmış ya da iptal edilmiş token tekrar geldi: aile ele geçirilmiş say, hepsini iptal et.
            var revoked = RevokeTokens(await RefreshTokens.ListUnrevokedByFamilyAsync(current.FamilyId, ct), TokenRevocationReason.ReuseDetected);
            await UnitOfWork.SaveChangesAsync(ct);
            Logger.LogWarning(
                "Refresh token tekrar kullanımı tespit edildi: kullanıcı {UserId}, aile {FamilyId}, iptal edilen {Count}",
                current.UserId,
                current.FamilyId,
                revoked);
            await NotifyFamilyRevokedAsync(current.UserId, current.FamilyId, ct);
            return IdentityErrors.RefreshTokenReuse;
        }

        if (!current.IsActive(Now))
        {
            return IdentityErrors.InvalidRefreshToken;
        }

        var user = await Users.FindByIdAsync(current.UserId.ToString());
        if (user is null || user.IsBanned)
        {
            RevokeTokens(await RefreshTokens.ListUnrevokedByFamilyAsync(current.FamilyId, ct), TokenRevocationReason.UserBanned);
            await UnitOfWork.SaveChangesAsync(ct);
            return user is null ? IdentityErrors.InvalidRefreshToken : IdentityErrors.UserBanned;
        }

        var (next, response) = await CreatePairAsync(user, current.FamilyId, current.Kind, ct);
        current.Revoke(Now, TokenRevocationReason.Rotated, next.Id);
        await UnitOfWork.SaveChangesAsync(ct);
        return response;
    }

    /// <inheritdoc />
    public override async Task<Result<int>> RevokeFamilyAsync(Guid userId, Guid familyId, CancellationToken ct = default)
    {
        var tokens = (await RefreshTokens.ListUnrevokedByFamilyAsync(familyId, ct)).Where(t => t.UserId == userId);
        var count = RevokeTokens(tokens, TokenRevocationReason.FamilyRevoked);
        await UnitOfWork.SaveChangesAsync(ct);
        Logger.LogInformation("Token ailesi iptal edildi: kullanıcı {UserId}, aile {FamilyId}, {Count} token", userId, familyId, count);
        await NotifyFamilyRevokedAsync(userId, familyId, ct);
        return count;
    }

    /// <inheritdoc />
    public override async Task<Result<int>> RevokeAllAsync(Guid userId, CancellationToken ct = default)
    {
        var count = RevokeTokens(await RefreshTokens.ListUnrevokedByUserAsync(userId, ct), TokenRevocationReason.LogoutAll);
        await UnitOfWork.SaveChangesAsync(ct);
        Logger.LogInformation("Tüm oturumlar kapatıldı (ajan dahil): kullanıcı {UserId}, {Count} token", userId, count);
        await NotifyAllRevokedAsync(userId, ct);
        return count;
    }
}
