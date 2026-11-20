using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Administration;

/// <summary>
/// Kişi hesabı çekirdeği (spec 0005'ten ayrıldı, spec 0009 veli bağlama ile paylaşılır): geçici şifreyle yeni hesap açma,
/// okul üyeliği ekleme ve rol atama — her biri audit'li (UserCreated, MemberAdded, RoleAssigned). Kilit almaz ve kaydetmez:
/// sıra (okul + kişi anahtarı, <see cref="AdminGuard.LockAsync"/>) ve tek kayıt çağıranın işidir; böylece kişi yönetimi ve
/// veli bağlama aynı kuralları tek işlemde uygular (plan 0009 Risks 3). Geçici şifre yalnızca dönüş değerindedir.
/// </summary>
public sealed class MemberAccounts
{
    private readonly AdminAudit audit;
    private readonly IUserRepository users;
    private readonly IRoleAssignmentRepository roleAssignments;
    private readonly ITenantMembershipRepository memberships;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITemporaryPasswordGenerator passwords;

    public MemberAccounts(
        AdminAudit audit,
        IUserRepository users,
        IRoleAssignmentRepository roleAssignments,
        ITenantMembershipRepository memberships,
        IPasswordHasher passwordHasher,
        ITemporaryPasswordGenerator passwords)
    {
        this.audit = audit;
        this.users = users;
        this.roleAssignments = roleAssignments;
        this.memberships = memberships;
        this.passwordHasher = passwordHasher;
        this.passwords = passwords;
    }

    /// <summary>Yeni kişi açar (telefon spec 0005 kuralıyla tek biçime çevrilir); bir kez gösterilecek geçici şifreyi döndürür.</summary>
    public (User User, string TemporaryPassword) CreateUser(Guid tenantId, Guid actorId, string email, string fullName, string? phone, DateTimeOffset now)
    {
        string temporaryPassword = this.passwords.Generate();
        PhoneNumber.TryNormalize(phone, out string? normalizedPhone);
        User user = User.Create(email, fullName, this.passwordHasher.Hash(temporaryPassword), now, normalizedPhone);
        this.users.Add(user);
        this.audit.Record(AuditAction.UserCreated, tenantId, actorId, nameof(User), user.Id);
        return (user, temporaryPassword);
    }

    public void AddMembership(Guid userId, Guid tenantId, Guid actorId, DateTimeOffset now)
    {
        this.memberships.Add(TenantMembership.Create(userId, tenantId, now));
        this.audit.Record(AuditAction.MemberAdded, tenantId, actorId, nameof(User), userId);
    }

    public void AddAssignments(Guid userId, Guid tenantId, Guid actorId, IEnumerable<(Role Role, Guid? BranchId)> plan, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);
        foreach ((Role role, Guid? branchId) in plan)
        {
            RoleAssignment assignment = role == Role.Founder
                ? RoleAssignment.Founder(userId, tenantId, now)
                : RoleAssignment.ForBranch(userId, tenantId, role, branchId!.Value, now);
            this.roleAssignments.Add(assignment);
            this.audit.Record(AuditAction.RoleAssigned, tenantId, actorId, nameof(RoleAssignment), assignment.Id);
        }
    }
}
