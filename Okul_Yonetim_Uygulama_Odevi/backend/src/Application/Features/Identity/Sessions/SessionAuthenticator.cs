using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Sessions;

/// <summary>
/// Her istekte imzası doğrulanmış erişim anahtarının oturumunu veritabanından kontrol eder (spec 0004, AC-4/AC-8):
/// oturum var, kişiye ait, iptal edilmemiş, 7 günlük kullanılmama süresi dolmamış ve anahtardaki kurum oturumun
/// şu anki kurumuyla aynı olmalı; kurum seçiliyse kişinin o kurumda en az bir rolü olmalı. Geçerse kurucu bilgisi ve
/// görebildiği şubelerle <see cref="CurrentSession"/> doldurulur; geçmezse istek reddedilir.
/// </summary>
public sealed class SessionAuthenticator
{
    private readonly IUserSessionRepository sessions;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly CurrentSession currentSession;
    private readonly TimeProvider timeProvider;

    public SessionAuthenticator(
        IUserSessionRepository sessions,
        IRoleAssignmentRepository roleAssignments,
        CurrentSession currentSession,
        TimeProvider timeProvider)
    {
        this.sessions = sessions;
        this.roleAssignments = roleAssignments;
        this.currentSession = currentSession;
        this.timeProvider = timeProvider;
    }

    public async Task<bool> AuthenticateAsync(Guid userId, Guid sessionId, Guid? tenantId, CancellationToken cancellationToken)
    {
        UserSession? session = await this.sessions.FindAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null
            || session.UserId != userId
            || !session.IsActive(this.timeProvider.GetUtcNow())
            || session.TenantId != tenantId)
        {
            return false;
        }

        bool isFounder = false;
        Guid[] branchIds = [];
        if (tenantId is { } tenant)
        {
            IReadOnlyList<RoleAssignment> assignments = await this.roleAssignments
                .ListForUserInTenantAsync(userId, tenant, cancellationToken)
                .ConfigureAwait(false);
            if (assignments.Count == 0)
            {
                return false;
            }

            isFounder = assignments.Any(a => a.Role == Role.Founder);
            branchIds = [.. assignments.Where(a => a.BranchId is not null).Select(a => a.BranchId!.Value).Distinct()];
        }

        this.currentSession.Authenticate(userId, sessionId, tenantId, isFounder, branchIds);
        return true;
    }
}
