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
/// Kimlik doğrulama akışları. Kayıt: kayıt ayarı açıksa (<see cref="IRegistrationSettingsService"/>) normal kullanıcı açılır — hiçbir
/// kayıt yönetici olmaz (yönetici <see cref="IAdminSeeder"/> ile tohumlanır); kullanıcı adı benzersizliğini Identity/veritabanı korur.
/// Giriş: kullanıcı adı ya da e-posta → kilit → parola → yasak kontrolü; başarıda son giriş kaydı + web token ailesi.
/// Yenileme <see cref="IRefreshTokenRotator"/>'a, çıkış <see cref="ITokenRevoker.RevokeAllAsync"/>'e devredilir.
/// </summary>
public sealed class AuthService : AuthServiceBase
{
    private readonly IRegistrationSettingsService _registration;
    private readonly ITokenIssuer _issuer;
    private readonly IRefreshTokenRotator _rotator;
    private readonly ITokenRevoker _revoker;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly ILogger<AuthService> _logger;

    /// <summary>Bağımlılıkları alır.</summary>
    /// <param name="users">Kullanıcı yöneticisi.</param>
    /// <param name="registration">Kayıt ayarı.</param>
    /// <param name="issuer">Token verici.</param>
    /// <param name="rotator">Refresh rotasyonu.</param>
    /// <param name="revoker">Token iptali.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    /// <param name="clock">Saat.</param>
    /// <param name="logger">Günlükleyici.</param>
    public AuthService(
        UserManager<AppUser> users,
        IRegistrationSettingsService registration,
        ITokenIssuer issuer,
        IRefreshTokenRotator rotator,
        ITokenRevoker revoker,
        ICurrentUser currentUser,
        IClock clock,
        ILogger<AuthService> logger)
        : base(users)
    {
        _registration = registration;
        _issuer = issuer;
        _rotator = rotator;
        _revoker = revoker;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<Result<RegistrationStatusResponse>> GetRegistrationStatusAsync(CancellationToken ct = default) =>
        new RegistrationStatusResponse { RegistrationOpen = await _registration.IsRegistrationOpenAsync(ct) };

    /// <inheritdoc />
    public override async Task<Result<TokenResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await _registration.IsRegistrationOpenAsync(ct))
        {
            _logger.LogWarning("Kayıt reddedildi: yeni kayıtlar kapalı.");
            return IdentityErrors.RegistrationClosed;
        }

        var user = new AppUser(request.UserName.Trim(), _clock.UtcNow);
        var created = await Users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return ToError(created);
        }

        user.RecordLogin(_clock.UtcNow);
        await Users.UpdateAsync(user);
        _logger.LogInformation("Yeni hesap açıldı: {UserId}.", user.Id);
        return (await _issuer.IssueAsync(user.Id, TokenClientKind.Web, ct)).Map(t => t.Tokens);
    }

    /// <inheritdoc />
    public override async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await FindByLoginAsync(request.UserName.Trim());
        if (user is null)
        {
            _logger.LogWarning("Başarısız giriş: bilinmeyen kullanıcı adı / e-posta.");
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
}
