namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Rol ataması: kişi + kurum + rol + (kurucu dışındaki rollerde) şube. Kurucu ataması kurum düzeyindedir (şube boş) ve
/// kurumun tüm şubelerini görmeyi sağlar; diğer roller yalnızca atandıkları şubeyi görür (BR-1).
/// </summary>
public sealed class RoleAssignment
{
    private RoleAssignment(Guid id, Guid userId, Guid tenantId, Role role, Guid? branchId, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.UserId = userId;
        this.TenantId = tenantId;
        this.Role = role;
        this.BranchId = branchId;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid TenantId { get; private set; }

    public Role Role { get; private set; }

    public Guid? BranchId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static RoleAssignment Founder(Guid userId, Guid tenantId, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), userId, tenantId, Role.Founder, null, now);

    public static RoleAssignment ForBranch(Guid userId, Guid tenantId, Role role, Guid branchId, DateTimeOffset now)
    {
        if (role == Role.Founder)
        {
            throw new ArgumentException("Kurucu rolü şubeye değil kuruma atanır.", nameof(role));
        }

        return new(Guid.CreateVersion7(now), userId, tenantId, role, branchId, now);
    }
}
