namespace OkulYonetim.Domain.Billing;

/// <summary>
/// İndirim/burs tanımı (spec 0015 gereksinim 2): ad (zorunlu, en fazla <see cref="NameMax"/>, okulda etkinler arasında harf farkı
/// gözetmeden tekil) ve yüzde (<see cref="MinPercent"/>–<see cref="MaxPercent"/>, tam sayı). Kurallar tarifeyle aynıdır; plan
/// açılıştaki adı, yüzdeyi ve tutarı kendisi saklar.
/// </summary>
public sealed class DiscountDefinition
{
    public const int NameMax = 100;
    public const int MinPercent = 1;
    public const int MaxPercent = 100;

    private DiscountDefinition(Guid id, Guid tenantId, string name, int percent, DateTimeOffset createdAt, Guid createdBy)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.Name = name;
        this.Percent = percent;
        this.IsActive = true;
        this.CreatedAt = createdAt;
        this.CreatedBy = createdBy;
        this.UpdatedAt = createdAt;
        this.UpdatedBy = createdBy;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public int Percent { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid UpdatedBy { get; private set; }

    public static bool IsValidPercent(int percent) => percent is >= MinPercent and <= MaxPercent;

    public static DiscountDefinition Create(Guid tenantId, string name, int percent, DateTimeOffset now, Guid actorId) =>
        new(Guid.CreateVersion7(now), tenantId, CleanName(name), CheckedPercent(percent), now, actorId);

    public void Update(string name, int percent, DateTimeOffset now, Guid actorId)
    {
        this.Name = CleanName(name);
        this.Percent = CheckedPercent(percent);
        this.UpdatedAt = now;
        this.UpdatedBy = actorId;
    }

    /// <summary>Etkin/pasif yapar; durum gerçekten değiştiyse <c>true</c>.</summary>
    public bool SetActive(bool active, DateTimeOffset now, Guid actorId)
    {
        if (this.IsActive == active)
        {
            return false;
        }

        this.IsActive = active;
        this.UpdatedAt = now;
        this.UpdatedBy = actorId;
        return true;
    }

    private static string CleanName(string name)
    {
        string clean = name?.Trim() ?? string.Empty;
        return clean.Length is 0 or > NameMax ? throw new ArgumentOutOfRangeException(nameof(name), "İndirim adı boş olamaz ve sınırı aşamaz.") : clean;
    }

    private static int CheckedPercent(int percent) =>
        IsValidPercent(percent) ? percent : throw new ArgumentOutOfRangeException(nameof(percent), "İndirim yüzdesi 1–100 olmalı.");
}
