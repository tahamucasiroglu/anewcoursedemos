using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Application.Security;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Identity.Application.Abstract;
using TahaKonsept.Modules.Identity.Application.Models;
using TahaKonsept.Modules.Identity.Domain.Entities;
using TahaKonsept.Modules.Identity.Domain.Enums;
using TahaKonsept.Shared.Contracts.Identity;

namespace TahaKonsept.Modules.Identity.Application.Base;

/// <summary>
/// Token servislerinin tabanı (<see cref="ITokenIssuer"/>, <see cref="ITokenRevoker"/>, <see cref="IRefreshTokenRotator"/>).
/// Ortak yardımcılar: <see cref="CreatePairAsync"/> (refresh kaydı ekler + JWT üretir, kaydetmez),
/// <see cref="RevokeTokens"/> (toplu iptal), <see cref="NotifyAllRevokedAsync"/> / <see cref="NotifyFamilyRevokedAsync"/>
/// (tüm <see cref="ISessionRevocationNotifier"/>'ları güvenle çağırır).
/// </summary>
public abstract class TokenServiceBase : ITokenIssuer, ITokenRevoker, IRefreshTokenRotator
{
    private readonly IEnumerable<ISessionRevocationNotifier> _notifiers;
    private readonly IAccessTokenFactory _accessTokens;
    private readonly IPermissionResolver _permissions;
    private readonly IClock _clock;
    private readonly JwtOptions _jwtOptions;

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
    protected TokenServiceBase(
        UserManager<AppUser> users,
        IRefreshTokenRepository refreshTokens,
        IIdentityUnitOfWork unitOfWork,
        IAccessTokenFactory accessTokens,
        IRefreshTokenGenerator generator,
        IPermissionResolver permissions,
        IClock clock,
        JwtOptions jwtOptions,
        IEnumerable<ISessionRevocationNotifier> notifiers,
        ILogger logger)
    {
        Users = users ?? throw new ArgumentNullException(nameof(users));
        RefreshTokens = refreshTokens ?? throw new ArgumentNullException(nameof(refreshTokens));
        UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _accessTokens = accessTokens ?? throw new ArgumentNullException(nameof(accessTokens));
        Generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));
        _notifiers = notifiers ?? [];
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Refresh token deposu.</summary>
    protected IRefreshTokenRepository RefreshTokens { get; }

    /// <summary>İş birimi.</summary>
    protected IIdentityUnitOfWork UnitOfWork { get; }

    /// <summary>Refresh token üretici/özetleyici.</summary>
    protected IRefreshTokenGenerator Generator { get; }

    /// <summary>Günlükleyici.</summary>
    protected ILogger Logger { get; }

    /// <summary>Kullanıcı yöneticisi.</summary>
    protected UserManager<AppUser> Users { get; }

    /// <summary>Şu an (UTC).</summary>
    protected DateTimeOffset Now => _clock.UtcNow;

    /// <inheritdoc />
    public abstract Task<Result<IssuedTokens>> IssueAsync(Guid userId, TokenClientKind kind, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<int>> RevokeFamilyAsync(Guid userId, Guid familyId, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<int>> RevokeAllAsync(Guid userId, CancellationToken ct = default);

    /// <inheritdoc />
    public abstract Task<Result<TokenResponse>> RotateAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Yeni refresh kaydını depoya ekler ve erişim token'ı üretir. Kaydetmez (çağıran <c>SaveChangesAsync</c> yapar).</summary>
    /// <param name="user">Kullanıcı.</param>
    /// <param name="familyId">Aile.</param>
    /// <param name="kind">İstemci türü.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Eklenen refresh kaydı ve istemci yanıtı.</returns>
    protected async Task<(RefreshToken Entity, TokenResponse Response)> CreatePairAsync(AppUser user, Guid familyId, RefreshTokenKind kind, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);
        var now = Now;
        var raw = Generator.Generate();
        var entity = RefreshToken.Issue(user.Id, Generator.Hash(raw), familyId, kind, now, TimeSpan.FromDays(Math.Max(1, _jwtOptions.RefreshTokenDays)));
        await RefreshTokens.AddAsync(entity, ct);

        var access = await _permissions.ResolveAsync(user);
        var token = _accessTokens.Create(new AccessTokenDescriptor(
            user.Id,
            user.UserName ?? string.Empty,
            access.Roles,
            access.Permissions,
            familyId,
            kind));

        return (entity, new TokenResponse
        {
            AccessToken = token.Token,
            AccessTokenExpiresAtUtc = token.ExpiresAtUtc,
            RefreshToken = raw,
            RefreshTokenExpiresAtUtc = entity.ExpiresAtUtc,
        });
    }

    /// <summary>Verilen token'ları iptal eder (kaydetmez).</summary>
    /// <param name="tokens">Token'lar.</param>
    /// <param name="reason">Neden.</param>
    /// <returns>Bu çağrıda iptal edilen sayısı.</returns>
    protected int RevokeTokens(IEnumerable<RefreshToken> tokens, TokenRevocationReason reason)
    {
        var now = Now;
        var count = 0;
        foreach (var token in tokens.Where(t => !t.IsRevoked))
        {
            token.Revoke(now, reason);
            count++;
        }

        return count;
    }

    /// <summary>Tüm bildirimcilere "tüm oturumlar iptal" der; hatalar loglanır, yutulur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected Task NotifyAllRevokedAsync(Guid userId, CancellationToken ct) =>
        NotifyAsync(n => n.OnAllSessionsRevokedAsync(userId, ct), userId);

    /// <summary>Tüm bildirimcilere "aile iptal" der; hatalar loglanır, yutulur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="familyId">Aile.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected Task NotifyFamilyRevokedAsync(Guid userId, Guid familyId, CancellationToken ct) =>
        NotifyAsync(n => n.OnTokenFamilyRevokedAsync(userId, familyId, ct), userId);

    private async Task NotifyAsync(Func<ISessionRevocationNotifier, Task> call, Guid userId)
    {
        foreach (var notifier in _notifiers)
        {
            try
            {
                await call(notifier);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogWarning(ex, "Oturum iptal bildirimi başarısız: {Notifier}, kullanıcı {UserId}", notifier.GetType().Name, userId);
            }
        }
    }
}
