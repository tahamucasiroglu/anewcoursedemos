namespace OkulYonetim.Domain.Organization;

/// <summary>
/// Kurum: sistemi kullanan müşteri okul. Verisi diğer kurumlardan tamamen ayrıdır (BR-4); kurumun her kaydı
/// kurum kimliğini taşır. Okul açma komutuyla oluşturulur (spec 0004).
/// </summary>
public sealed class Tenant
{
    private Tenant(Guid id, string name, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.Name = name;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Etkin mi (spec 0007): pasif okulda kimse giremez; veri silinmez.</summary>
    public bool IsActive { get; private set; } = true;

    public void Deactivate() => this.IsActive = false;

    public void Activate() => this.IsActive = true;

    public static Tenant Create(string name, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), name.Trim(), now);
}
