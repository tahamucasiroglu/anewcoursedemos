namespace OkulYonetim.Domain.Organization;

/// <summary>
/// Dönem: "2026-2027 Güz" (spec 0008). Okul geneli; başlangıç ve bitiş günü (Türkiye günü, saatsiz) dahil. Başlangıç
/// bitişten önce olmalı ve dönem en fazla <see cref="MaxDays"/> gün sürer (uç günler dahil sayılır). Aynı okulda iki
/// dönemin günleri çakışamaz (<see cref="Overlaps"/>: bitiş günü öbürünün başlangıç günüyse çakışır). Silinmez,
/// pasifleştirilmez; yalnızca adı ve tarihleri düzenlenir.
/// </summary>
public sealed class Term
{
    public const int MaxDays = 366;

    private Term(Guid id, Guid tenantId, string name, DateOnly startDate, DateOnly endDate, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.Name = name;
        this.StartDate = startDate;
        this.EndDate = endDate;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Term Create(Guid tenantId, string name, DateOnly startDate, DateOnly endDate, DateTimeOffset now)
    {
        EnsureValidRange(startDate, endDate);
        return new(Guid.CreateVersion7(now), tenantId, name.Trim(), startDate, endDate, now);
    }

    /// <summary>Uç günler dahil gün sayısı (1 Eylül 2026 – 1 Eylül 2027: 366).</summary>
    public static int LengthInDays(DateOnly startDate, DateOnly endDate) => endDate.DayNumber - startDate.DayNumber + 1;

    public static bool IsValidRange(DateOnly startDate, DateOnly endDate) =>
        startDate < endDate && LengthInDays(startDate, endDate) <= MaxDays;

    public void Update(string name, DateOnly startDate, DateOnly endDate)
    {
        EnsureValidRange(startDate, endDate);
        this.Name = name.Trim();
        this.StartDate = startDate;
        this.EndDate = endDate;
    }

    public bool Contains(DateOnly day) => day >= this.StartDate && day <= this.EndDate;

    public bool Overlaps(DateOnly startDate, DateOnly endDate) => startDate <= this.EndDate && endDate >= this.StartDate;

    private static void EnsureValidRange(DateOnly startDate, DateOnly endDate)
    {
        if (!IsValidRange(startDate, endDate))
        {
            throw new ArgumentOutOfRangeException(nameof(endDate), "Dönem başlangıcı bitişten önce olmalı ve dönem en fazla 366 gün sürmeli.");
        }
    }
}
