namespace OkulYonetim.Domain.Organization;

/// <summary>
/// Şube: aynı kurumun fiziksel lokasyonu (sınıf şubesi değil). Her zaman bir kuruma bağlıdır; kurucu kurumun tüm
/// şubelerini, diğer roller yalnızca atandıkları şubeleri görür (BR-1, BR-4).
/// </summary>
public sealed class Branch
{
    private Branch(Guid id, Guid tenantId, string name, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.Name = name;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Branch Create(Guid tenantId, string name, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, name.Trim(), now);
}
