using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Sessions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Commands.Login;

/// <summary>
/// Giriş (spec 0004, gereksinim 3/6/7): önce e-postanın kilidi kontrol edilir; şifre yanlışsa ya da e-posta yoksa aynı
/// mesajla reddedilir ve sayaç artar (3. hatada 15 dk kilit). Doğruysa sayaç sıfırlanır, oturum açılır; tek okul varsa
/// seçilir, birden çoksa okul seçimi istenir. Her sonuç audit kaydına geçer (denenen e-posta, IP). Aynı e-postanın
/// girişleri sıraya sokulur (kilit kontrolü → şifre → sayaç tek işlemde): eşzamanlı isteklerle 3 deneme sınırı aşılamaz
/// (review H-1).
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IUserRepository users;
    private readonly ILoginThrottleRepository throttles;
    private readonly IUserSessionRepository sessions;
    private readonly IAuditLog audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenService tokens;
    private readonly IClientInfo client;
    private readonly UserTenantDirectory tenantDirectory;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<LoginCommandHandler> logger;

    public LoginCommandHandler(
        IUserRepository users,
        ILoginThrottleRepository throttles,
        IUserSessionRepository sessions,
        IAuditLog audit,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokens,
        IClientInfo client,
        UserTenantDirectory tenantDirectory,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<LoginCommandHandler> logger)
    {
        this.users = users;
        this.throttles = throttles;
        this.sessions = sessions;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.passwordHasher = passwordHasher;
        this.tokens = tokens;
        this.client = client;
        this.tenantDirectory = tenantDirectory;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string email = EmailAddress.Normalize(request.Email);
        IExclusiveScope scope = await this.BeginAsync("login:" + email, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            try
            {
                AuthResult result = await this.TryLoginAsync(email, request.Password, cancellationToken).ConfigureAwait(false);
                await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (AuthorizationException)
            {
                // Ret de kalıcıdır: sayaç ve audit kaydı yazılmış olmalı.
                await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task<IExclusiveScope> BeginAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await this.unitOfWork.BeginExclusiveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            this.logger.LogWarning("Giriş reddedildi: aynı e-postaya çok sayıda eşzamanlı istek");
            throw new AuthorizationException(await this.localization.GetAsync(IdentityMessages.TooManyRequests).ConfigureAwait(false));
        }
    }

    private async Task<AuthResult> TryLoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        string? ip = this.client.IpAddress;
        LoginThrottle? throttle = await this.throttles.FindAsync(email, cancellationToken).ConfigureAwait(false);

        if (throttle is not null && throttle.IsLocked(now))
        {
            this.audit.Add(AuditEntry.Record(AuditAction.LoginFailed, now, attemptedEmail: email, ipAddress: ip));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogWarning("Giriş reddedildi: hesap kilitli");
            throw new AuthorizationException(await this.localization.GetAsync(IdentityMessages.AccountLocked).ConfigureAwait(false));
        }

        User? user = await this.users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        bool valid = this.passwordHasher.Verify(user?.PasswordHash, password) && user is not null;
        if (!valid)
        {
            await this.RejectAsync(email, throttle, now, ip, cancellationToken).ConfigureAwait(false);
        }

        throttle?.Reset();
        IReadOnlyList<TenantSummary> tenants = await this.tenantDirectory.ListAsync(user!.Id, cancellationToken).ConfigureAwait(false);
        Guid? tenantId = tenants.Count == 1 ? tenants[0].Id : null;
        string refreshToken = this.tokens.CreateRefreshToken();
        UserSession session = UserSession.Open(user.Id, tenantId, this.tokens.HashRefreshToken(refreshToken), now, ip);
        this.sessions.Add(session);
        this.audit.Add(AuditEntry.Record(AuditAction.LoginSucceeded, now, tenantId, user.Id, targetType: nameof(UserSession), targetId: session.Id, ipAddress: ip));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.logger.LogInformation("Giriş başarılı: kişi {UserId}, oturum {SessionId}, kurum {TenantId}", user.Id, session.Id, tenantId);
        IssuedAccessToken access = this.tokens.IssueAccessToken(user.Id, session.Id, tenantId, now);
        return new AuthResult(access.Token, access.ExpiresAt, refreshToken, tenantId is null, tenants);
    }

    private async Task RejectAsync(string email, LoginThrottle? throttle, DateTimeOffset now, string? ip, CancellationToken cancellationToken)
    {
        if (throttle is null)
        {
            throttle = LoginThrottle.For(email);
            this.throttles.Add(throttle);
        }

        bool locked = throttle.RegisterFailure(now);
        this.audit.Add(AuditEntry.Record(AuditAction.LoginFailed, now, attemptedEmail: email, ipAddress: ip));
        if (locked)
        {
            this.audit.Add(AuditEntry.Record(AuditAction.LoginLockedOut, now, attemptedEmail: email, ipAddress: ip));
        }

        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogWarning("Giriş reddedildi: hatalı bilgi (art arda {FailedCount}. hata, kilit: {Locked})", throttle.FailedCount, locked);
        string key = locked ? IdentityMessages.AccountLocked : IdentityMessages.InvalidCredentials;
        throw new AuthorizationException(await this.localization.GetAsync(key).ConfigureAwait(false));
    }
}
