using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Commands.Logout;

/// <summary>
/// Çıkış (spec 0004, gereksinim 9): oturum iptal edilir; o oturumun erişim ve yenileme anahtarlarıyla yapılan sonraki
/// her istek reddedilir. Audit kaydına geçer.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly ICurrentSession currentSession;
    private readonly IUserSessionRepository sessions;
    private readonly IAuditLog audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClientInfo client;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<LogoutCommandHandler> logger;

    public LogoutCommandHandler(
        ICurrentSession currentSession,
        IUserSessionRepository sessions,
        IAuditLog audit,
        IUnitOfWork unitOfWork,
        IClientInfo client,
        TimeProvider timeProvider,
        ILogger<LogoutCommandHandler> logger)
    {
        this.currentSession = currentSession;
        this.sessions = sessions;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.client = client;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (this.currentSession.SessionId is not { } sessionId)
        {
            return false;
        }

        UserSession? session = await this.sessions.FindAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return false;
        }

        DateTimeOffset now = this.timeProvider.GetUtcNow();
        session.Revoke(now);
        this.audit.Add(AuditEntry.Record(AuditAction.Logout, now, session.TenantId, session.UserId, targetType: nameof(UserSession), targetId: session.Id, ipAddress: this.client.IpAddress));
        await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this.logger.LogInformation("Çıkış: oturum {SessionId} kapatıldı", session.Id);
        return true;
    }
}
