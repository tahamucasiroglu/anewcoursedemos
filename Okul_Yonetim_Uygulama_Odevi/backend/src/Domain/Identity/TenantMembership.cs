namespace OkulYonetim.Domain.Identity;

/// <summary>
/// Üyelik: kişinin bir okuldaki varlığı ve durumu (spec 0005). Pasifleştirme rolleri silmeden kişinin o okula erişimini
/// keser; yeniden etkinleştirince eski rolleri geçerli olur. Kişinin başka okullardaki üyeliği etkilenmez.
/// </summary>
public sealed class TenantMembership
{
    private TenantMembership(Guid id, Guid userId, Guid tenantId, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.UserId = userId;
        this.TenantId = tenantId;
        this.CreatedAt = createdAt;
        this.IsActive = true;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid TenantId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static TenantMembership Create(Guid userId, Guid tenantId, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), userId, tenantId, now);

    public void Deactivate() => this.IsActive = false;

    public void Activate() => this.IsActive = true;
}
