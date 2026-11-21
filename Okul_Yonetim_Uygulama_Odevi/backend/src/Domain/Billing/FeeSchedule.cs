namespace OkulYonetim.Domain.Billing;

/// <summary>
/// Ücret tarifesi (spec 0015 gereksinim 1): okulun adlandırılmış yıllık tutarı ("2026-2027 Eğitim Ücreti", 85.000,00 TL). Ad zorunlu
/// (en fazla <see cref="NameMax"/>; okulda etkinler arasında harf farkı gözetmeden tekil — uygulamada denetlenir), tutar sıfırdan
/// büyük, en fazla iki ondalık ve <see cref="MoneyRules.MaxFeeAmount"/>. Pasif tarife yeni planda seçilemez; düzenleme açılmış
/// planları değiştirmez (plan açılıştaki adı ve tutarı kendisi saklar).
/// </summary>
public sealed class FeeSchedule
{
    public const int NameMax = 100;

    private FeeSchedule(Guid id, Guid tenantId, string name, decimal amount, DateTimeOffset createdAt, Guid createdBy)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.Name = name;
        this.Amount = amount;
        this.IsActive = true;
        this.CreatedAt = createdAt;
        this.CreatedBy = createdBy;
        this.UpdatedAt = createdAt;
        this.UpdatedBy = createdBy;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public decimal Amount { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid UpdatedBy { get; private set; }

    public static bool IsValidAmount(decimal amount) => MoneyRules.IsPositiveMoney(amount) && amount <= MoneyRules.MaxFeeAmount;

    public static FeeSchedule Create(Guid tenantId, string name, decimal amount, DateTimeOffset now, Guid actorId) =>
        new(Guid.CreateVersion7(now), tenantId, CleanName(name), CheckedAmount(amount), now, actorId);

    public void Update(string name, decimal amount, DateTimeOffset now, Guid actorId)
    {
        this.Name = CleanName(name);
        this.Amount = CheckedAmount(amount);
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
        return clean.Length is 0 or > NameMax ? throw new ArgumentOutOfRangeException(nameof(name), "Tarife adı boş olamaz ve sınırı aşamaz.") : clean;
    }

    private static decimal CheckedAmount(decimal amount) =>
        IsValidAmount(amount) ? amount : throw new ArgumentOutOfRangeException(nameof(amount), "Tarife tutarı geçersiz.");
}
