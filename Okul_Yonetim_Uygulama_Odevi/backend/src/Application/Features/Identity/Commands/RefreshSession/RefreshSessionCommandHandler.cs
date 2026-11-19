using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Sessions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Commands.RefreshSession;

/// <summary>
/// Oturumu yeniler (spec 0004, gereksinim 9): yenileme anahtarının özeti bir oturumla eşleşmeli, oturum iptal edilmemiş
/// ve 7 gündür kullanılmamış olmamalı. Anahtar döndürülür (eskisi artık geçmez), süre uzar; yeni erişim anahtarı
/// oturumun seçili kurumunu taşır. Geçersizse "oturumunuz sona erdi" ile reddedilir.
/// </summary>
public sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, AuthResult>
{
    private readonly IUserSessionRepository sessions;
    private readonly IUnitOfWork unitOfWork;
    private readonly ITokenService tokens;
    private readonly UserTenantDirectory tenantDirectory;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<RefreshSessionCommandHandler> logger;

    public RefreshSessionCommandHandler(
        IUserSessionRepository sessions,
        IUnitOfWork unitOfWork,
        ITokenService tokens,
        UserTenantDirectory tenantDirectory,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<RefreshSessionCommandHandler> logger)
    {
        this.sessions = sessions;
        this.unitOfWork = unitOfWork;
        this.tokens = tokens;
        this.tenantDirectory = tenantDirectory;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<AuthResult> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        DateTimeOffset now = this.timeProvider.GetUtcNow();
        UserSession? session = string.IsNullOrEmpty(request.RefreshToken)
            ? null
            : await this.sessions.FindByRefreshTokenHashAsync(this.tokens.HashRefreshToken(request.RefreshToken), cancellationToken).ConfigureAwait(false);
        if (session is null || !session.IsActive(now))
        {
            this.logger.LogWarning("Oturum yenileme reddedildi: geçersiz ya da süresi dolmuş anahtar");
            throw new AuthorizationException(await this.localization.GetAsync(IdentityMessages.InvalidSession).ConfigureAwait(false));
        }

        string refreshToken = this.tokens.CreateRefreshToken();
        session.Rotate(this.tokens.HashRefreshToken(refreshToken), now);
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.logger.LogInformation("Oturum yenilendi: oturum {SessionId}", session.Id);
        IReadOnlyList<TenantSummary> tenants = await this.tenantDirectory.ListAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        IssuedAccessToken access = this.tokens.IssueAccessToken(session.UserId, session.Id, session.TenantId, now);
        return new AuthResult(access.Token, access.ExpiresAt, refreshToken, session.TenantId is null, tenants);
    }
}
