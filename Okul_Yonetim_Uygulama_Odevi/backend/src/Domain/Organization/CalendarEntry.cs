namespace OkulYonetim.Domain.Organization;

/// <summary>
/// Akademik takvim kaydı (spec 0008): bir döneme bağlı, okul geneli tatil/sınav/etkinlik; başlangıç ve bitiş günü (aynı
/// gün = tek günlük kayıt), iki gün de dönemin içinde. Silme yumuşaktır (<see cref="DeletedAt"/>): silinen kayıt hiçbir
/// ekranda görünmez, audit kaydı kalır.
/// </summary>
public sealed class CalendarEntry
{
    private CalendarEntry(Guid id, Guid tenantId, Guid termId, string title, CalendarEntryKind kind, DateOnly startDate, DateOnly endDate, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.TermId = termId;
        this.Title = title;
        this.Kind = kind;
        this.StartDate = startDate;
        this.EndDate = endDate;
        this.CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid TermId { get; private set; }

    public string Title { get; private set; }

    public CalendarEntryKind Kind { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static CalendarEntry Create(Guid tenantId, Guid termId, string title, CalendarEntryKind kind, DateOnly startDate, DateOnly endDate, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, termId, title.Trim(), kind, startDate, endDate, now);

    public void Update(string title, CalendarEntryKind kind, DateOnly startDate, DateOnly endDate)
    {
        this.Title = title.Trim();
        this.Kind = kind;
        this.StartDate = startDate;
        this.EndDate = endDate;
    }

    public void Delete(DateTimeOffset now) => this.DeletedAt ??= now;
}
