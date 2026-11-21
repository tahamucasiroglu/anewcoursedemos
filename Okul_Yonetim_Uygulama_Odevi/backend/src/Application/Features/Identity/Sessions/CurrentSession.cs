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

    /// <summary>
    /// Arka plan işinin okul oturumu (spec 0015 hatırlatma işçisi; plan 0015 Risks 2): kişi yok, kimlik doğrulanmış sayılmaz, okul
    /// seçili ve bütün şubeler görünür (kurucu gibi). Yalnızca <c>OverdueReminderRunner</c> çağırır (architecture testi denetler);
    /// her okul ayrı DI kapsamında çalışır, filtreler yine okul sınırını uygular — filtre delme yok.
    /// </summary>
    internal void AuthenticateSystem(Guid tenantId)
    {
        this.IsAuthenticated = false;
        this.UserId = null;
        this.SessionId = null;
        this.TenantId = tenantId;
        this.IsFounder = true;
        this.IsPlatformAdmin = false;
        this.VisibleBranchIds = [];
    }
}
