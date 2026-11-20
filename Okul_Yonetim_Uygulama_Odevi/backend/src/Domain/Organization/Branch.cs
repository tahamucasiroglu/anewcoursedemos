namespace OkulYonetim.Domain.Organization;

/// <summary>
/// Şube: aynı kurumun fiziksel lokasyonu (sınıf şubesi değil). Her zaman bir kuruma bağlıdır; kurucu kurumun tüm
/// şubelerini, diğer roller yalnızca atandıkları şubeleri görür (BR-1, BR-4). Silinmez; pasifleştirilir (spec 0005):
/// pasif şube yeni atamada seçilemez ve kurucu olmayanlara görünmez.
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

    public string? Address { get; private set; }

    public string? Phone { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Branch Create(Guid tenantId, string name, DateTimeOffset now, string? address = null, string? phone = null) =>
        new(Guid.CreateVersion7(now), tenantId, name.Trim(), now) { Address = Clean(address), Phone = phone };

    public void UpdateDetails(string name, string? address, string? phone)
    {
        this.Name = name.Trim();
        this.Address = Clean(address);
        this.Phone = phone;
    }

    public void Deactivate() => this.IsActive = false;

    public void Activate() => this.IsActive = true;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
