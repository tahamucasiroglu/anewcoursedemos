using Microsoft.Extensions.Logging;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Identity.Commands.PlatformAdmin;

/// <summary>
/// Yetki ver / al ve doğrulama sıfırlama: kişi yoksa false (hiçbir şey değişmez); işlem kişinin bütün açık oturumlarını
/// kapatır ve audit'e komut kaynağıyla yazılır. Kişinin giriş sırasında yürür (girişle yarışmaz).
/// </summary>
public sealed class PlatformAdminCommandHandler : IRequestHandler<PlatformAdminCommand, bool>
{
    private readonly IUserRepository users;
    private readonly IUserSessionRepository sessions;
    private readonly IAuditLog audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<PlatformAdminCommandHandler> logger;

    public PlatformAdminCommandHandler(IUserRepository users, IUserSessionRepository sessions, IAuditLog audit, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<PlatformAdminCommandHandler> logger)
    {
        this.users = users;
        this.sessions = sessions;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<bool> Handle(PlatformAdminCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        string email = EmailAddress.Normalize(request.Email);
        IExclusiveScope scope = await this.unitOfWork.BeginExclusiveAsync(["login:" + email], cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            User? user = await this.users.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return false;
            }

            DateTimeOffset now = this.timeProvider.GetUtcNow();
            AuditAction action;
            switch (request.Action)
            {
                case PlatformAdminAction.Grant:
                    user.GrantPlatformAdmin();
                    action = AuditAction.PlatformAdminGranted;
                    break;
                case PlatformAdminAction.Revoke:
                    user.RevokePlatformAdmin();
                    action = AuditAction.PlatformAdminRevoked;
                    break;
                default:
                    user.ResetTwoFactor();
                    action = AuditAction.TwoFactorReset;
                    break;
            }

            await this.sessions.RevokeAllForUserAsync(user.Id, now, cancellationToken).ConfigureAwait(false);
            this.audit.Add(AuditEntry.Record(action, now, targetType: nameof(User), targetId: user.Id, origin: request.Origin));
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Platform yöneticisi komutu: {Action}, kişi {UserId}; açık oturumlar kapatıldı", request.Action, user.Id);
            return true;
        }
    }
}
