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
/// Kimlik doğrulama akışları. Kurulum yalnızca hiç kullanıcı yokken çalışır (süreç içi kilitle yarış önlenir) ve
/// hesabı <see cref="RoleNames.Admin"/> rolüne ekler. Giriş: kilit → parola → yasak kontrolü; başarıda son giriş kaydı +
/// web token ailesi. Yenileme <see cref="IRefreshTokenRotator"/>'a, çıkış <see cref="ITokenRevoker.RevokeAllAsync"/>'e devredilir.
/// </summary>
public sealed class AuthService : AuthServiceBase
{
    private static readonly SemaphoreSlim SetupLock = new(1, 1);

    private readonly RoleManager<AppRole> _roles;
    private readonly IUserReadRepository _userQueries;
    private readonly ITokenIssuer _issuer;
    private readonly IRefreshTokenRotator _rotator;
    private readonly ITokenRevoker _revoker;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<AuthService> _logger;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    /// <param name="roles">Rol yöneticisi.</param>
    /// <param name="userQueries">Kullanıcı sorguları.</param>
    /// <param name="issuer">Token verici.</param>
    /// <param name="rotator">Refresh rotasyonu.</param>
    /// <param name="revoker">Token iptali.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Günlükleyici.</param>
    public AuthService(
        UserManager<AppUser> users,
        RoleManager<AppRole> roles,
        IUserReadRepository userQueries,
        ITokenIssuer issuer,
        IRefreshTokenRotator rotator,
        ITokenRevoker revoker,
        ICurrentUser currentUser,
        IClock clock,
        ILogger<AuthService> logger)
        : base(users)
    {
        _roles = roles;
        _userQueries = userQueries;
        _issuer = issuer;
        _rotator = rotator;
        _revoker = revoker;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result<SetupStatusResponse>> GetSetupStatusAsync(CancellationToken ct = default) =>
        new SetupStatusResponse { SetupRequired = await _userQueries.CountAsync(ct) == 0 };

    /// <inheritdoc />
    public override async Task<Result<TokenResponse>> SetupAsync(SetupRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await SetupLock.WaitAsync(ct);
        try
        {
            if (await _userQueries.CountAsync(ct) > 0)
            {
                return IdentityErrors.SetupCompleted;
            }

            var roleResult = await EnsureAdminRoleAsync();
            if (roleResult.IsFailure)
            {
                return roleResult.Error;
            }

            var user = new AppUser(request.UserName.Trim(), _clock.UtcNow);
            var created = await Users.CreateAsync(user, request.Password);
            if (!created.Succeeded)
            {
                return ToError(created);
            }

            var inRole = await Users.AddToRoleAsync(user, RoleNames.Admin);
            if (!inRole.Succeeded)
            {
                return ToError(inRole);
            }

            user.RecordLogin(_clock.UtcNow);
            await Users.UpdateAsync(user);
            _logger.LogInformation("İlk kurulum tamamlandı: yönetici {UserName} oluşturuldu.", user.UserName);
            return (await _issuer.IssueAsync(user.Id, TokenClientKind.Web, ct)).Map(t => t.Tokens);
        }
        finally
        {
            SetupLock.Release();
        }
    }

    /// <inheritdoc />
    public override async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await Users.FindByNameAsync(request.UserName.Trim());
        if (user is null)
        {
            _logger.LogWarning("Başarısız giriş: bilinmeyen kullanıcı adı.");
            return IdentityErrors.InvalidCredentials;
        }

        if (await Users.IsLockedOutAsync(user))
        {
            return IdentityErrors.LockedOut;
        }

        if (!await Users.CheckPasswordAsync(user, request.Password))
        {
            await Users.AccessFailedAsync(user);
            _logger.LogWarning("Başarısız giriş: hatalı parola, kullanıcı {UserId}.", user.Id);
            return IdentityErrors.InvalidCredentials;
        }

        if (user.IsBanned)
        {
            _logger.LogWarning("Engellenmiş kullanıcı giriş denedi: {UserId}.", user.Id);
            return IdentityErrors.UserBanned;
        }

        await Users.ResetAccessFailedCountAsync(user);
        user.RecordLogin(_clock.UtcNow);
        await Users.UpdateAsync(user);
        _logger.LogInformation("Giriş yapıldı: {UserId}.", user.Id);
        return (await _issuer.IssueAsync(user.Id, TokenClientKind.Web, ct)).Map(t => t.Tokens);
    }

    /// <inheritdoc />
    public override Task<Result<TokenResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _rotator.RotateAsync(request.RefreshToken, ct);
    }

    /// <inheritdoc />
    public override async Task<Result> LogoutAllAsync(CancellationToken ct = default)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return IdentityErrors.NotAuthenticated;
        }

        var result = await _revoker.RevokeAllAsync(userId, ct);
        return result.IsSuccess ? Result.Success() : result.Error;
    }

    private async Task<Result> EnsureAdminRoleAsync()
    {
        if (await _roles.RoleExistsAsync(RoleNames.Admin))
        {
            return Result.Success();
        }

        var created = await _roles.CreateAsync(new AppRole(RoleNames.Admin));
        return created.Succeeded ? Result.Success() : ToError(created);
    }
}
