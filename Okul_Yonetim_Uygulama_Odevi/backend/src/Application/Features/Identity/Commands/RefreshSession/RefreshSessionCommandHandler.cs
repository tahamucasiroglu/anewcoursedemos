using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Sessions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Commands.RefreshSession;

/// <summary>
/// Oturumu yeniler (spec 0004, gereksinim 9): yenileme anahtarının özeti bir oturumla eşleşmeli, oturum iptal edilmemiş
/// ve 7 gündür kullanılmamış olmamalı. Anahtar döndürülür, süre uzar; yeni erişim anahtarı oturumun seçili kurumunu taşır.
/// Aynı anahtarla gelen istekler sıraya sokulur (review M-1). Bir önceki anahtar 30 sn içinde gelirse (birden çok sekme
/// aynı anda yeniledi) yalnızca yeni erişim anahtarı verilir, yenileme anahtarı değişmez. Daha sonra gelirse anahtar
/// çalınmış sayılır: oturum iptal edilir ve audit'e yazılır. Geçersizse "oturumunuz sona erdi" ile reddedilir.
/// </summary>
public sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, AuthResult>
{
    private readonly IUserSessionRepository sessions;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IAuditLog audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly ITokenService tokens;
    private readonly IClientInfo client;
    private readonly UserTenantDirectory tenantDirectory;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<RefreshSessionCommandHandler> logger;

    public RefreshSessionCommandHandler(
        IUserSessionRepository sessions,
        IRoleAssignmentRepository roleAssignments,
        IAuditLog audit,
        IUnitOfWork unitOfWork,
        ITokenService tokens,
        IClientInfo client,
        UserTenantDirectory tenantDirectory,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<RefreshSessionCommandHandler> logger)
    {
        this.sessions = sessions;
        this.roleAssignments = roleAssignments;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.tokens = tokens;
        this.client = client;
        this.tenantDirectory = tenantDirectory;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AuthResult> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            throw await this.InvalidAsync().ConfigureAwait(false);
        }

        string presentedHash = this.tokens.HashRefreshToken(request.RefreshToken);
        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync("refresh:" + presentedHash, cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            this.logger.LogWarning("Oturum yenileme reddedildi: aynı anahtarla çok sayıda eşzamanlı istek");
            throw new AuthorizationException(await this.localization.GetAsync(IdentityMessages.TooManyRequests).ConfigureAwait(false));
        }

        await using (scope.ConfigureAwait(false))
        {
            AuthResult? result = await this.TryRefreshAsync(presentedHash, cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result ?? throw await this.InvalidAsync().ConfigureAwait(false);
        }
    }

    private async Task<AuthResult?> TryRefreshAsync(string presentedHash, CancellationToken cancellationToken)
    {
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        UserSession? session = await this.sessions.FindByRefreshTokenHashAsync(presentedHash, cancellationToken).ConfigureAwait(false);
        if (session is not null)
        {
            if (!session.IsActive(now) || !await this.HasSchoolAccessAsync(session, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            string refreshToken = this.tokens.CreateRefreshToken();
            session.Rotate(this.tokens.HashRefreshToken(refreshToken), now);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Oturum yenilendi: oturum {SessionId}", session.Id);
            return await this.IssueAsync(session, refreshToken, now, cancellationToken).ConfigureAwait(false);
        }

        UserSession? previous = await this.sessions.FindByPreviousRefreshTokenHashAsync(presentedHash, cancellationToken).ConfigureAwait(false);
        if (previous is null || !previous.IsActive(now))
        {
            return null;
        }

        if (previous.IsWithinRotationGrace(presentedHash, now))
        {
            this.logger.LogInformation("Oturum eşzamanlı yenilendi (bir önceki anahtar, tolerans içinde): oturum {SessionId}", previous.Id);
            return await this.IssueAsync(previous, refreshToken: null, now, cancellationToken).ConfigureAwait(false);
        }

        previous.Revoke(now);
        this.audit.Add(AuditEntry.Record(AuditAction.RefreshTokenReused, now, previous.TenantId, previous.UserId, targetType: nameof(UserSession), targetId: previous.Id, ipAddress: this.client.IpAddress));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogWarning("Eski yenileme anahtarı yeniden kullanıldı; oturum {SessionId} iptal edildi", previous.Id);
        return null;
    }

    // Seçili okulda pasifleştirilen ya da bütün rolleri alınan kişi o okul için yenileme alamaz (spec 0005): istemci
    // Giriş'e döner, yeniden girişte etkin okullarını görür.
    private async Task<bool> HasSchoolAccessAsync(UserSession session, CancellationToken cancellationToken) =>
        session.TenantId is not { } tenantId
        || (await this.roleAssignments.ListForUserInTenantAsync(session.UserId, tenantId, cancellationToken).ConfigureAwait(false)).Count > 0;

    private async Task<AuthResult> IssueAsync(UserSession session, string? refreshToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        IReadOnlyList<TenantSummary> tenants = await this.tenantDirectory.ListAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        IssuedAccessToken access = this.tokens.IssueAccessToken(session.UserId, session.Id, session.TenantId, now);
        return new AuthResult(access.Token, access.ExpiresAt, refreshToken, session.TenantId is null, tenants);
    }

    private async Task<AuthorizationException> InvalidAsync()
    {
        this.logger.LogWarning("Oturum yenileme reddedildi: geçersiz ya da süresi dolmuş anahtar");
        return new AuthorizationException(await this.localization.GetAsync(IdentityMessages.InvalidSession).ConfigureAwait(false));
    }
}
