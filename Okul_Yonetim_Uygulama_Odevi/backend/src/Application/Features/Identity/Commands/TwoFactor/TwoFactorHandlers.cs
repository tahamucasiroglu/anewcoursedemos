using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Sessions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Commands.TwoFactor;

/// <summary>
/// İki adımlı doğrulama (spec 0007): şifresi doğrulanmış platform yöneticisi, 5 dakikalık bekleme anahtarıyla kurulum
/// yapar ya da 6 haneli kodu girer; doğru kodla oturum (kodla açılmış olarak) açılır. Yanlış kod girişteki şifre
/// hatalarıyla aynı kilit sayacına eklenir (3 → 15 dk); aynı kod ikinci kez kabul edilmez. Kod ve anahtar loglanmaz.
/// İşlemler kişinin giriş sırasında ("login:") yürür; kararlar sıra içinde taze verilerle verilir.
/// </summary>
public sealed class TwoFactorHandlers :
    IRequestHandler<BeginTwoFactorSetupCommand, TwoFactorSetupResult>,
    IRequestHandler<ConfirmTwoFactorSetupCommand, AuthResult>,
    IRequestHandler<VerifyTwoFactorCommand, AuthResult>
{
    private readonly ITwoFactorChallengeRepository challenges;
    private readonly IUserRepository users;
    private readonly IUserSessionRepository sessions;
    private readonly ILoginThrottleRepository throttles;
    private readonly ITotp totp;
    private readonly ISecretProtector protector;
    private readonly ITokenService tokens;
    private readonly UserTenantDirectory tenantDirectory;
    private readonly IAuditLog audit;
    private readonly IClientInfo client;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<TwoFactorHandlers> logger;

    public TwoFactorHandlers(
        ITwoFactorChallengeRepository challenges,
        IUserRepository users,
        IUserSessionRepository sessions,
        ILoginThrottleRepository throttles,
        ITotp totp,
        ISecretProtector protector,
        ITokenService tokens,
        UserTenantDirectory tenantDirectory,
        IAuditLog audit,
        IClientInfo client,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<TwoFactorHandlers> logger)
    {
        this.challenges = challenges;
        this.users = users;
        this.sessions = sessions;
        this.throttles = throttles;
        this.totp = totp;
        this.protector = protector;
        this.tokens = tokens;
        this.tenantDirectory = tenantDirectory;
        this.audit = audit;
        this.client = client;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<TwoFactorSetupResult> Handle(BeginTwoFactorSetupCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string hash = this.tokens.HashRefreshToken(request.Challenge ?? string.Empty);
        Guid? ownerId = await this.challenges.FindOwnerIdAsync(hash, cancellationToken).ConfigureAwait(false);
        string email = ownerId is { } id ? await this.users.FindEmailAsync(id, cancellationToken).ConfigureAwait(false) ?? string.Empty : string.Empty;
        TwoFactorSetupResult? result = null;
        IExclusiveScope scope = await this.LockAsync(email, hash, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            (TwoFactorChallenge? challenge, User? user) = await this.LoadAsync(hash, ownerId, now, cancellationToken).ConfigureAwait(false);
            if (challenge is not null && user is not null && user.TwoFactorEnabledAt is null && this.protector.IsConfigured)
            {
                string secret = challenge.PendingSecretCipher is { } pending ? this.protector.Unprotect(pending) : this.totp.GenerateSecret();
                challenge.SetPendingSecret(this.protector.Protect(secret));
                result = new TwoFactorSetupResult(secret, this.totp.ProvisioningUri(secret, user.Email));
                await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return result ?? throw new AuthorizationException(await this.localization.GetAsync(IdentityMessages.TwoFactorChallengeInvalid).ConfigureAwait(false));
    }

    public Task<AuthResult> Handle(ConfirmTwoFactorSetupCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.CompleteAsync(request.Challenge, request.Code, setup: true, cancellationToken);
    }

    public Task<AuthResult> Handle(VerifyTwoFactorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return this.CompleteAsync(request.Challenge, request.Code, setup: false, cancellationToken);
    }

    private async Task<AuthResult> CompleteAsync(string? challengeToken, string? code, bool setup, CancellationToken cancellationToken)
    {
        string hash = this.tokens.HashRefreshToken(challengeToken ?? string.Empty);
        Guid? ownerId = await this.challenges.FindOwnerIdAsync(hash, cancellationToken).ConfigureAwait(false);
        string email = ownerId is { } id ? await this.users.FindEmailAsync(id, cancellationToken).ConfigureAwait(false) ?? string.Empty : string.Empty;
        string? failure = null;
        bool expired = false;
        AuthResult? result = null;
        IExclusiveScope scope = await this.LockAsync(email, hash, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            string? ip = this.client.IpAddress;
            (TwoFactorChallenge? challenge, User? user) = await this.LoadAsync(hash, ownerId, now, cancellationToken).ConfigureAwait(false);
            string? cipher = setup ? challenge?.PendingSecretCipher : user?.TwoFactorSecretCipher;
            if (challenge is null || user is null || cipher is null || !this.protector.IsConfigured || setup != (user.TwoFactorEnabledAt is null))
            {
                expired = true;
            }
            else
            {
                LoginThrottle? throttle = await this.throttles.FindAsync(user.Email, cancellationToken).ConfigureAwait(false);
                long step = 0;
                bool valid = !(throttle?.IsLocked(now) ?? false)
                    && this.totp.Verify(this.protector.Unprotect(cipher), code ?? string.Empty, now, out step)
                    && (setup || user.AcceptTwoFactorStep(step));
                if (throttle is not null && throttle.IsLocked(now))
                {
                    this.audit.Add(AuditEntry.Record(AuditAction.TwoFactorFailed, now, actorUserId: user.Id, attemptedEmail: user.Email, ipAddress: ip));
                    failure = IdentityMessages.AccountLocked;
                }
                else if (!valid)
                {
                    if (throttle is null)
                    {
                        throttle = LoginThrottle.For(user.Email);
                        this.throttles.Add(throttle);
                    }

                    bool locked = throttle.RegisterFailure(now);
                    this.audit.Add(AuditEntry.Record(AuditAction.TwoFactorFailed, now, actorUserId: user.Id, attemptedEmail: user.Email, ipAddress: ip));
                    if (locked)
                    {
                        this.audit.Add(AuditEntry.Record(AuditAction.LoginLockedOut, now, actorUserId: user.Id, attemptedEmail: user.Email, ipAddress: ip));
                    }

                    failure = locked ? IdentityMessages.AccountLocked : IdentityMessages.TwoFactorCodeWrong;
                }
                else
                {
                    throttle?.Reset();
                    if (setup)
                    {
                        user.EnableTwoFactor(cipher, now, step);
                        this.audit.Add(AuditEntry.Record(AuditAction.TwoFactorEnabled, now, actorUserId: user.Id, targetType: nameof(User), targetId: user.Id, ipAddress: ip));
                    }
                    else
                    {
                        this.audit.Add(AuditEntry.Record(AuditAction.TwoFactorSucceeded, now, actorUserId: user.Id, targetType: nameof(User), targetId: user.Id, ipAddress: ip));
                    }

                    challenge.MarkUsed(now);
                    result = await this.OpenSessionAsync(user, now, ip, cancellationToken).ConfigureAwait(false);
                }
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        if (expired)
        {
            this.logger.LogWarning("İki adımlı doğrulama reddedildi: bekleme geçersiz ya da süresi dolmuş");
            throw new AuthorizationException(await this.localization.GetAsync(IdentityMessages.TwoFactorChallengeInvalid).ConfigureAwait(false));
        }

        if (failure is not null)
        {
            this.logger.LogWarning("İki adımlı doğrulama reddedildi: kişi {UserId} ({Reason})", ownerId, failure);
            throw new BusinessException(await this.localization.GetAsync(failure).ConfigureAwait(false));
        }

        this.logger.LogInformation("İki adımlı doğrulama başarılı: kişi {UserId} (kurulum: {Setup})", ownerId, setup);
        return result!;
    }

    private async Task<AuthResult> OpenSessionAsync(User user, DateTimeOffset now, string? ip, CancellationToken cancellationToken)
    {
        IReadOnlyList<TenantSummary> tenants = await this.tenantDirectory.ListAsync(user.Id, cancellationToken).ConfigureAwait(false);
        Guid? tenantId = tenants.Count == 1 ? tenants[0].Id : null;
        string refreshToken = this.tokens.CreateRefreshToken();
        UserSession session = UserSession.Open(user.Id, tenantId, this.tokens.HashRefreshToken(refreshToken), now, ip, twoFactorVerified: true);
        this.sessions.Add(session);
        this.audit.Add(AuditEntry.Record(AuditAction.LoginSucceeded, now, tenantId, user.Id, targetType: nameof(UserSession), targetId: session.Id, ipAddress: ip));
        IssuedAccessToken access = this.tokens.IssueAccessToken(user.Id, session.Id, tenantId, now);
        // Okulu olmayan platform yöneticisi okul seçmeden Hoş geldin'e (panel bağlantısı) gider; birden çok okulda seçim.
        return new AuthResult(access.Token, access.ExpiresAt, refreshToken, tenants.Count > 1, tenants);
    }

    private async Task<(TwoFactorChallenge? Challenge, User? User)> LoadAsync(string hash, Guid? ownerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (ownerId is not { } id)
        {
            return (null, null);
        }

        TwoFactorChallenge? challenge = await this.challenges.FindByHashAsync(hash, cancellationToken).ConfigureAwait(false);
        User? user = await this.users.FindByIdFreshAsync(id, cancellationToken).ConfigureAwait(false);
        return challenge is not null && challenge.IsUsable(now) && user is not null && user.IsPlatformAdmin ? (challenge, user) : (null, null);
    }

    private async Task<IExclusiveScope> LockAsync(string email, string hash, CancellationToken cancellationToken)
    {
        try
        {
            return await this.unitOfWork.BeginExclusiveAsync(["login:" + email, "two-factor:" + hash], cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.GetAsync(IdentityMessages.TooManyRequests).ConfigureAwait(false));
        }
    }
}
