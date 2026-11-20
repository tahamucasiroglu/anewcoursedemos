using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Persistence.Contexts;

/// <summary>
/// Uygulamanın tek veritabanı bağlamı (PostgreSQL, snake_case). Kurum ve şube izolasyonu (S1) burada, global sorgu
/// filtresiyle uygulanır: kurum yalnızca oturumdaki kurumu, şube yalnızca o kurumun ve (kurucu değilse) kişinin atandığı
/// şubeleri (kurucu olmayana yalnızca etkin olanları — spec 0005), rol ataması ve üyelik yalnızca o kurumunkileri gösterir. Oturum doğrulanmadıysa kurum boştur ve filtreler hiçbir kayıt
/// göstermez. Filtre değerleri sorgu anında <see cref="ICurrentSession"/>'dan okunur. Filtreyi delen okumalar
/// (<c>IgnoreQueryFilters</c>) yalnızca izinli listedeki depo metotlarındadır (T5).
/// </summary>
public sealed class OkulDbContext : DbContext
{
    private readonly ICurrentSession? session;

    public OkulDbContext(DbContextOptions<OkulDbContext> options, ICurrentSession? session = null)
        : base(options)
    {
        this.session = session;
    }

    public DbSet<Tenant> Tenants => this.Set<Tenant>();

    public DbSet<Branch> Branches => this.Set<Branch>();

    public DbSet<User> Users => this.Set<User>();

    public DbSet<RoleAssignment> RoleAssignments => this.Set<RoleAssignment>();

    public DbSet<TenantMembership> Memberships => this.Set<TenantMembership>();

    public DbSet<UserSession> UserSessions => this.Set<UserSession>();

    public DbSet<LoginThrottle> LoginThrottles => this.Set<LoginThrottle>();

    public DbSet<AuditEntry> AuditEntries => this.Set<AuditEntry>();

    public DbSet<PasswordResetToken> PasswordResetTokens => this.Set<PasswordResetToken>();

    public DbSet<TwoFactorChallenge> TwoFactorChallenges => this.Set<TwoFactorChallenge>();

    private Guid? CurrentTenantId => this.session?.TenantId;

    private bool CurrentIsFounder => this.session?.IsFounder ?? false;

    private IReadOnlyCollection<Guid> CurrentBranchIds => this.session?.VisibleBranchIds ?? [];

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OkulDbContext).Assembly);

        modelBuilder.Entity<Tenant>().HasQueryFilter(t => t.Id == this.CurrentTenantId);
        modelBuilder.Entity<Branch>().HasQueryFilter(b =>
            b.TenantId == this.CurrentTenantId && (this.CurrentIsFounder || (b.IsActive && this.CurrentBranchIds.Contains(b.Id))));
        modelBuilder.Entity<RoleAssignment>().HasQueryFilter(r => r.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<TenantMembership>().HasQueryFilter(m => m.TenantId == this.CurrentTenantId);
    }
}
