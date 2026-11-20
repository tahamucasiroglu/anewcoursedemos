using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Account;

/// <summary>
/// Hesabım ve şifre işlemleri (spec 0006). Profil ve şifre değiştirme yalnızca kişinin kendisi için (oturumdaki kişi;
/// başka kişinin kimliği alınmaz). Şifre değiştirmede yanlış mevcut şifre girişle aynı kilit sayacına eklenir (aynı
/// "login:e-posta" sırası). Şifremi unuttum her durumda aynı yanıtı verir; e-posta arka planda gönderilir. Bağlantı yalnızca
/// özetiyle saklanır, tek kullanımlıktır; şifre değişince kişinin açıkta kalan bağlantıları geçersizleşir.
/// </summary>
public sealed class AccountHandlers :
    IRequestHandler<GetMyProfileQuery, MyProfile>,
    IRequestHandler<UpdateMyProfileCommand, MyProfile>,
    IRequestHandler<ChangePasswordCommand, bool>,
    IRequestHandler<RequestPasswordResetCommand, string>,
    IRequestHandler<CheckPasswordResetLinkQuery, bool>,
    IRequestHandler<ResetPasswordWithLinkCommand, bool>
{
    /// <summary>Aynı e-postaya 15 dakikada en fazla bu kadar bağlantı gönderilir (spec 0006 gereksinim 5).</summary>
    public const int MaxLinksPerWindow = 3;

    public static readonly TimeSpan LinkWindow = TimeSpan.FromMinutes(15);

    private readonly ICurrentSession session;
    private readonly IUserRepository users;
    private readonly IUserSessionRepository sessions;
    private readonly ILoginThrottleRepository throttles;
    private readonly IPasswordResetTokenRepository resetTokens;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenService tokens;
    private readonly IEmailOutbox emailOutbox;
    private readonly IAppLinks links;
    private readonly IAuditLog audit;
    private readonly IClientInfo client;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<AccountHandlers> logger;

    public AccountHandlers(
        ICurrentSession session,
        IUserRepository users,
        IUserSessionRepository sessions,
        ILoginThrottleRepository throttles,
        IPasswordResetTokenRepository resetTokens,
        IPasswordHasher passwordHasher,
        ITokenService tokens,
        IEmailOutbox emailOutbox,
        IAppLinks links,
        IAuditLog audit,
        IClientInfo client,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<AccountHandlers> logger)
    {
        this.session = session;
        this.users = users;
        this.sessions = sessions;
        this.throttles = throttles;
        this.resetTokens = resetTokens;
        this.passwordHasher = passwordHasher;
        this.tokens = tokens;
        this.emailOutbox = emailOutbox;
        this.links = links;
        this.audit = audit;
        this.client = client;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<MyProfile> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        User user = await this.CurrentUserAsync(cancellationToken).ConfigureAwait(false);
        return View(user);
    }

    public async Task<MyProfile> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        User user = await this.CurrentUserAsync(cancellationToken).ConfigureAwait(false);
        PhoneNumber.TryNormalize(request.Phone, out string? phone);
        user.UpdateProfile(request.FullName, phone);
        this.Record(AuditAction.ProfileUpdated, user.Id, targetId: user.Id);
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation("Profil güncellendi: kişi {UserId}", user.Id);
        return View(user);
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Guid userId = this.session.UserId ?? throw new AuthorizationException(await this.localization.GetAsync(AccountMessages.NotSignedIn).ConfigureAwait(false));
        Guid sessionId = this.session.SessionId ?? throw new AuthorizationException(await this.localization.GetAsync(AccountMessages.NotSignedIn).ConfigureAwait(false));
        string email = await this.users.FindEmailAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthorizationException(await this.localization.GetAsync(AccountMessages.NotSignedIn).ConfigureAwait(false));
        string? failure = null;
        User user;

        // Kişinin şifresini değiştiren bütün yollarla aynı sıra (review round 2, Medium-1): giriş/şifre değiştirme
        // ("login:") ve kurucu sıfırlaması / bağlantıyla yenileme ("member-admin:"). Sıra içinde kişi ve oturum taze okunur:
        // beklerken şifre başka yolla değiştiyse eski şifre artık tutmaz; oturum kapandıysa işlem yapılmaz.
        IExclusiveScope scope = await this.LockAsync(["login:" + email, "member-admin:" + userId.ToString("N")], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            user = await this.users.FindByIdFreshAsync(userId, cancellationToken).ConfigureAwait(false)
                ?? throw new AuthorizationException(await this.localization.GetAsync(AccountMessages.NotSignedIn).ConfigureAwait(false));
            if (!await this.sessions.IsOpenAsync(sessionId, now, cancellationToken).ConfigureAwait(false))
            {
                throw new AuthorizationException(await this.localization.GetAsync(AccountMessages.NotSignedIn).ConfigureAwait(false));
            }

            LoginThrottle? throttle = await this.throttles.FindAsync(user.Email, cancellationToken).ConfigureAwait(false);
            if (throttle is not null && throttle.IsLocked(now))
            {
                this.Record(AuditAction.PasswordChangeFailed, user.Id, targetId: user.Id);
                failure = AccountMessages.AccountLocked;
            }
            else if (!this.passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
            {
                if (throttle is null)
                {
                    throttle = LoginThrottle.For(user.Email);
                    this.throttles.Add(throttle);
                }

                bool locked = throttle.RegisterFailure(now);
                this.Record(AuditAction.PasswordChangeFailed, user.Id, targetId: user.Id);
                if (locked)
                {
                    this.audit.Add(AuditEntry.Record(AuditAction.LoginLockedOut, now, actorUserId: user.Id, attemptedEmail: user.Email, ipAddress: this.client.IpAddress));
                }

                failure = locked ? AccountMessages.AccountLocked : AccountMessages.CurrentPasswordWrong;
            }
            else
            {
                throttle?.Reset();
                user.ChangePasswordHash(this.passwordHasher.Hash(request.NewPassword));
                await this.sessions.RevokeAllForUserExceptAsync(user.Id, sessionId, now, cancellationToken).ConfigureAwait(false);
                await this.resetTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
                this.Record(AuditAction.PasswordChanged, user.Id, targetId: user.Id);
            }

            // Ret de kalıcıdır: sayaç ve audit kaydı yazılır (girişteki gibi).
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        if (failure is not null)
        {
            this.logger.LogWarning("Şifre değiştirme reddedildi: kişi {UserId} ({Reason})", user.Id, failure);
            throw new BusinessException(await this.localization.GetAsync(failure).ConfigureAwait(false));
        }

        this.logger.LogInformation("Şifre değiştirildi: kişi {UserId}; diğer oturumlar kapatıldı", user.Id);
        return true;
    }

    public async Task<string> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string email = EmailAddress.Normalize(request.Email);
        EmailMessage? outgoing = null;
        IExclusiveScope scope = await this.LockAsync(["reset-request:" + email], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            User? user = await this.users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                // Kayıtlı yolla aynı veritabanı okumaları (review F-4): yanıt süresi hesabın varlığını ele vermesin.
                await this.resetTokens.CountCreatedSinceAsync(Guid.Empty, now - LinkWindow, cancellationToken).ConfigureAwait(false);
                await this.resetTokens.RevokeAllForUserAsync(Guid.Empty, now, cancellationToken).ConfigureAwait(false);
                this.audit.Add(AuditEntry.Record(AuditAction.PasswordResetRequested, now, attemptedEmail: email, ipAddress: this.client.IpAddress));
                this.logger.LogInformation("Şifre yenileme isteği: kayıtlı olmayan e-posta, e-posta gönderilmedi");
            }
            else if (await this.resetTokens.CountCreatedSinceAsync(user.Id, now - LinkWindow, cancellationToken).ConfigureAwait(false) >= MaxLinksPerWindow)
            {
                this.Record(AuditAction.PasswordResetRequested, user.Id, targetId: user.Id);
                this.logger.LogWarning("Şifre yenileme isteği: kişi {UserId} için 15 dakikalık sınır doldu, e-posta gönderilmedi", user.Id);
            }
            else
            {
                await this.resetTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
                string token = this.tokens.CreateRefreshToken();
                PasswordResetToken stored = PasswordResetToken.Create(user.Id, this.tokens.HashRefreshToken(token), now);
                this.resetTokens.Add(stored);
                this.Record(AuditAction.PasswordResetRequested, user.Id, targetId: stored.Id);
                string subject = await this.localization.GetAsync(AccountMessages.ResetEmailSubject).ConfigureAwait(false);
                outgoing = PasswordResetEmail.Build(user.Id, user.Email, user.FullName, subject, this.links.PasswordReset(token));
                this.logger.LogInformation("Şifre yenileme bağlantısı üretildi: kişi {UserId}", user.Id);
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        // Kayıt kalıcı olduktan sonra kuyruğa (review F-3): kayıt başarısızsa geçersiz bağlantı içeren e-posta gitmez.
        if (outgoing is not null)
        {
            this.emailOutbox.Enqueue(outgoing);
        }

        return await this.localization.GetAsync(AccountMessages.ResetRequested).ConfigureAwait(false);
    }

    public async Task<bool> Handle(CheckPasswordResetLinkQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return false;
        }

        PasswordResetToken? token = await this.resetTokens.FindByHashAsync(this.tokens.HashRefreshToken(request.Token), cancellationToken).ConfigureAwait(false);
        return token is not null && token.IsUsable(this.timeProvider.GetUtcNow());
    }

    public async Task<bool> Handle(ResetPasswordWithLinkCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string hash = this.tokens.HashRefreshToken(request.Token);
        // Sıradan önce yalnızca sahibini öğren (izlemesiz): sıra içindeki okuma veritabanından taze gelsin (review F-1).
        Guid? ownerId = await this.resetTokens.FindOwnerIdAsync(hash, cancellationToken).ConfigureAwait(false);
        User? owner = ownerId is { } id ? await this.users.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) : null;
        List<string> keys = ["reset-link:" + hash];
        if (owner is not null)
        {
            // Kişinin şifresini değiştiren her yolla aynı sıra: kendi değişikliği ("login:") ve kurucu sıfırlaması ("member-admin:").
            keys.Add("login:" + owner.Email);
            keys.Add("member-admin:" + owner.Id.ToString("N"));
        }

        bool valid;
        IExclusiveScope scope = await this.LockAsync(keys, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();

            // Sıra içinde ilk (izlenen) okuma: önceki isteğin "kullanıldı" ya da başka yolun "iptal" kaydını görür; aynı bağlantı
            // aynı anda iki kez kullanılamaz, şifre değişirken bekleyen bağlantı isteği de geçersiz olur.
            PasswordResetToken? token = owner is null ? null : await this.resetTokens.FindByHashAsync(hash, cancellationToken).ConfigureAwait(false);
            valid = token is not null && owner is not null && token.IsUsable(now);
            if (!valid)
            {
                this.audit.Add(AuditEntry.Record(AuditAction.PasswordResetLinkInvalid, now, actorUserId: owner?.Id, targetType: nameof(PasswordResetToken), targetId: token?.Id, ipAddress: this.client.IpAddress));
            }
            else
            {
                owner!.ChangePasswordHash(this.passwordHasher.Hash(request.NewPassword));
                token!.MarkUsed(now);
                await this.resetTokens.RevokeAllForUserAsync(owner.Id, now, cancellationToken).ConfigureAwait(false);
                await this.sessions.RevokeAllForUserAsync(owner.Id, now, cancellationToken).ConfigureAwait(false);
                (await this.throttles.FindAsync(owner.Email, cancellationToken).ConfigureAwait(false))?.Reset();
                this.Record(AuditAction.PasswordResetCompleted, owner.Id, targetId: owner.Id);
            }

            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!valid)
        {
            this.logger.LogWarning("Şifre yenileme reddedildi: geçersiz ya da süresi dolmuş bağlantı");
            throw new BusinessException(await this.localization.GetAsync(AccountMessages.ResetLinkInvalid).ConfigureAwait(false));
        }

        this.logger.LogInformation("Şifre bağlantıyla yenilendi: kişi {UserId}; bütün oturumlar kapatıldı", owner!.Id);
        return true;
    }

    private static MyProfile View(User user) => new(user.FullName, user.Email, user.Phone);

    private void Record(AuditAction action, Guid actorId, Guid? targetId) =>
        this.audit.Add(AuditEntry.Record(action, this.timeProvider.GetUtcNow(), this.session.TenantId, actorId, targetType: nameof(User), targetId: targetId, ipAddress: this.client.IpAddress));

    private async Task<User> CurrentUserAsync(CancellationToken cancellationToken)
    {
        User? user = this.session.UserId is { } id ? await this.users.FindByIdAsync(id, cancellationToken).ConfigureAwait(false) : null;
        return user ?? throw new AuthorizationException(await this.localization.GetAsync(AccountMessages.NotSignedIn).ConfigureAwait(false));
    }

    private async Task<IExclusiveScope> LockAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken)
    {
        try
        {
            return await this.unitOfWork.BeginExclusiveAsync(keys, cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.GetAsync(AccountMessages.TooManyRequests).ConfigureAwait(false));
        }
    }
}
