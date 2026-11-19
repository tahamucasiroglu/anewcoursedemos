using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Identity.Sessions;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Commands.SelectTenant;

/// <summary>
/// Okul seçimi / değiştirme (spec 0004, gereksinim 4): kişinin o okulda rolü yoksa "okul bulunamadı" (başka okulun
/// varlığı sızdırılmaz). Oturumun kurumu değişir; eski kurumu taşıyan erişim anahtarı artık geçmez. Şifre sorulmaz.
/// </summary>
public sealed class SelectTenantCommandHandler : IRequestHandler<SelectTenantCommand, AuthResult>
{
    private readonly ICurrentSession currentSession;
    private readonly IUserSessionRepository sessions;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly IAuditLog audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly ITokenService tokens;
    private readonly IClientInfo client;
    private readonly UserTenantDirectory tenantDirectory;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SelectTenantCommandHandler> logger;

    public SelectTenantCommandHandler(
        ICurrentSession currentSession,
        IUserSessionRepository sessions,
        IRoleAssignmentRepository roleAssignments,
        IAuditLog audit,
        IUnitOfWork unitOfWork,
        ITokenService tokens,
        IClientInfo client,
        UserTenantDirectory tenantDirectory,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<SelectTenantCommandHandler> logger)
    {
        this.currentSession = currentSession;
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

    public async Task<AuthResult> Handle(SelectTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        UserSession? session = this.currentSession.SessionId is { } sessionId
            ? await this.sessions.FindAsync(sessionId, cancellationToken).ConfigureAwait(false)
            : null;
        IReadOnlyList<RoleAssignment> assignments = session is null
            ? []
            : await this.roleAssignments.ListForUserInTenantAsync(session.UserId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (session is null || assignments.Count == 0)
        {
            throw new NotFoundException(await this.localization.GetAsync(IdentityMessages.TenantNotFound).ConfigureAwait(false));
        }

        DateTimeOffset now = this.timeProvider.GetUtcNow();
        session.SelectTenant(request.TenantId, now);
        this.audit.Add(AuditEntry.Record(AuditAction.TenantSelected, now, request.TenantId, session.UserId, targetType: "Tenant", targetId: request.TenantId, ipAddress: this.client.IpAddress));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        this.logger.LogInformation("Okul seçildi: oturum {SessionId}, kurum {TenantId}", session.Id, request.TenantId);
        IReadOnlyList<TenantSummary> tenants = await this.tenantDirectory.ListAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        IssuedAccessToken access = this.tokens.IssueAccessToken(session.UserId, session.Id, request.TenantId, now);
        return new AuthResult(access.Token, access.ExpiresAt, null, false, tenants);
    }
}
