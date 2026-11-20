using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Attendance;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;
using OkulYonetim.Domain.Organization;
using OkulYonetim.Domain.Students;

namespace OkulYonetim.Persistence.Contexts;

/// <summary>
/// Uygulamanın tek veritabanı bağlamı (PostgreSQL, snake_case). Kurum ve şube izolasyonu (S1) burada, global sorgu
/// filtresiyle uygulanır: kurum yalnızca oturumdaki kurumu, şube yalnızca o kurumun ve (kurucu değilse) kişinin atandığı
/// şubeleri (kurucu olmayana yalnızca etkin olanları — spec 0005), rol ataması, üyelik, sınıf seviyesi, dönem ve takvim kaydı
/// (silinmemiş) yalnızca o kurumunkileri, sınıf ve öğrenci yalnızca görünür şubelerinkileri (spec 0008, 0009), veli bağı ve teslim kişisi yalnızca o kurumunkileri, sınıf öğretmeni ataması ve yoklama görünür şubelerinkileri, yoklama satırı
/// ve veli bildirimi o kurumunkileri gösterir (spec 0010); duyuru, onay, konuşma, mesaj ve mesai saatleri o kurumunkileri (spec 0011). Oturum doğrulanmadıysa kurum boştur ve filtreler hiçbir kayıt
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

    public DbSet<GradeLevel> GradeLevels => this.Set<GradeLevel>();

    public DbSet<Term> Terms => this.Set<Term>();

    public DbSet<Classroom> Classrooms => this.Set<Classroom>();

    public DbSet<CalendarEntry> CalendarEntries => this.Set<CalendarEntry>();

    public DbSet<Student> Students => this.Set<Student>();

    public DbSet<StudentGuardian> StudentGuardians => this.Set<StudentGuardian>();

    public DbSet<PickupPerson> PickupPersons => this.Set<PickupPerson>();

    public DbSet<ClassroomTeacher> ClassroomTeachers => this.Set<ClassroomTeacher>();

    public DbSet<AttendanceSheet> AttendanceSheets => this.Set<AttendanceSheet>();

    public DbSet<AttendanceEntry> AttendanceEntries => this.Set<AttendanceEntry>();

    public DbSet<GuardianNotice> GuardianNotices => this.Set<GuardianNotice>();

    public DbSet<Announcement> Announcements => this.Set<Announcement>();

    public DbSet<AnnouncementRecipient> AnnouncementRecipients => this.Set<AnnouncementRecipient>();

    public DbSet<AnnouncementStudent> AnnouncementStudents => this.Set<AnnouncementStudent>();

    public DbSet<ConsentResponse> ConsentResponses => this.Set<ConsentResponse>();

    public DbSet<Conversation> Conversations => this.Set<Conversation>();

    public DbSet<ConversationMessage> ConversationMessages => this.Set<ConversationMessage>();

    public DbSet<QuietHours> QuietHours => this.Set<QuietHours>();

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
        modelBuilder.Entity<GradeLevel>().HasQueryFilter(g => g.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<Term>().HasQueryFilter(t => t.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<CalendarEntry>().HasQueryFilter(e => e.TenantId == this.CurrentTenantId && e.DeletedAt == null);

        // Sınıf, şubesinin görünürlüğünü miras alır (plan 0008 Risks 2): şube filtresi iç sorguya da uygulanır — kurucu
        // okulun bütün şubelerini, diğer roller yalnızca atandıkları etkin şubeleri görür; kural tek yerde kalır.
        modelBuilder.Entity<Classroom>().HasQueryFilter(c =>
            c.TenantId == this.CurrentTenantId && this.Branches.Any(b => b.Id == c.BranchId));

        // Öğrenci de şubesinin görünürlüğünü miras alır (spec 0009, plan 0009): kurucu okulun bütün öğrencilerini, diğer
        // roller yalnızca atandıkları etkin şubelerinkileri görür. Velinin "yalnızca kendi çocuğu" duvarı bunun üstüne,
        // bağlar üzerinden uygulanır (veli uçları öğrenciyi yalnızca bağıyla okur). Bağ ve teslim kişisi kurum filtreli.
        modelBuilder.Entity<Student>().HasQueryFilter(s =>
            s.TenantId == this.CurrentTenantId && this.Branches.Any(b => b.Id == s.BranchId));
        modelBuilder.Entity<StudentGuardian>().HasQueryFilter(g => g.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<PickupPerson>().HasQueryFilter(p => p.TenantId == this.CurrentTenantId);

        // Spec 0010: sınıf öğretmeni ataması ve yoklama, sınıf gibi şubesinin görünürlüğünü miras alır; yoklama satırı ve veli
        // bildirimi kurum filtreli. Velinin "yalnızca kendi çocuğunun bildirimi" duvarı bağ üzerinden uygulanır (plan 0010 Risks 6).
        modelBuilder.Entity<ClassroomTeacher>().HasQueryFilter(t =>
            t.TenantId == this.CurrentTenantId && this.Branches.Any(b => b.Id == t.BranchId));
        modelBuilder.Entity<AttendanceSheet>().HasQueryFilter(s =>
            s.TenantId == this.CurrentTenantId && this.Branches.Any(b => b.Id == s.BranchId));
        modelBuilder.Entity<AttendanceEntry>().HasQueryFilter(e => e.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<GuardianNotice>().HasQueryFilter(n => n.TenantId == this.CurrentTenantId);

        // Spec 0011: duyuru, alıcı, kapsanan öğrenci, onay cevabı, konuşma, mesaj ve mesai saatleri yalnızca kurum filtreli.
        // Şube ve ilişki duvarı CommunicationGuard'dadır (velinin çocukları farklı şubelerde olabilir, BR-2): veli yalnızca
        // alıcısı olduğu duyuruyu, konuşmayı yalnızca iki tarafı, sonuçları yalnızca yayınlayan ve kurucu görür (plan 0011 Risks 3).
        modelBuilder.Entity<Announcement>().HasQueryFilter(a => a.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<AnnouncementRecipient>().HasQueryFilter(r => r.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<AnnouncementStudent>().HasQueryFilter(s => s.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<ConsentResponse>().HasQueryFilter(c => c.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<Conversation>().HasQueryFilter(c => c.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<ConversationMessage>().HasQueryFilter(m => m.TenantId == this.CurrentTenantId);
        modelBuilder.Entity<QuietHours>().HasQueryFilter(q => q.TenantId == this.CurrentTenantId);
    }
}
