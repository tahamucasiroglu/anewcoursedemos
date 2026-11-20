namespace OkulYonetim.Domain.Organization;

/// <summary>
/// Sınıf seviyesi (kademe): "5. sınıf", "hazırlık", "4 yaş grubu" (spec 0008). Okul kendi seviyelerini ad ve sıra
/// numarasıyla tanımlar; liste sıra numarasına göre dizilir. Silinmez, pasifleştirilir: pasif seviye yeni sınıfta
/// seçilemez, onu kullanan mevcut sınıflar olduğu gibi kalır.
/// </summary>
public sealed class GradeLevel
{
    private GradeLevel(Guid id, Guid tenantId, string name, int sortOrder, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.Name = name;
        this.SortOrder = sortOrder;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    public static GradeLevel Create(Guid tenantId, string name, int sortOrder, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, name.Trim(), sortOrder, now);

    public void Update(string name, int sortOrder)
    {
        this.Name = name.Trim();
        this.SortOrder = sortOrder;
    }

    public void Deactivate() => this.IsActive = false;

    public void Activate() => this.IsActive = true;
}
