using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Identity.Sessions;

/// <summary>
/// İstek başına oturum bilgisi (<see cref="ICurrentSession"/> uygulaması). Yalnızca <see cref="SessionAuthenticator"/>
/// doldurur: oturum veritabanında doğrulanmadan kişi, kurum ve şubeler boş kalır — filtreler hiçbir kaydı göstermez.
/// </summary>
public sealed class CurrentSession : ICurrentSession
{
    public bool IsAuthenticated { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? SessionId { get; private set; }

    public Guid? TenantId { get; private set; }

    public bool IsFounder { get; private set; }

    public bool IsPlatformAdmin { get; private set; }

    public IReadOnlyCollection<Guid> VisibleBranchIds { get; private set; } = [];

    internal void Authenticate(Guid userId, Guid sessionId, Guid? tenantId, bool isFounder, IReadOnlyCollection<Guid> branchIds, bool isPlatformAdmin = false)
    {
        this.IsPlatformAdmin = isPlatformAdmin;
        this.IsAuthenticated = true;
        this.UserId = userId;
        this.SessionId = sessionId;
        this.TenantId = tenantId;
        this.IsFounder = isFounder;
        this.VisibleBranchIds = branchIds;
    }
}
