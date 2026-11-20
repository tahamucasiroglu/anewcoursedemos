namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Öğretmenin mesai saatleri (spec 0011 C-6, BR-8): okul + öğretmen başına tek satır; başlangıç ve bitiş (Türkiye saati,
/// 15 dakikalık adım, başlangıç bitişten önce, aynı gün) ve iş günleri. Satır yoksa sessiz mod kapalıdır (her veli mesajı
/// bildirim üretir). <see cref="IsWorkingTime"/>: seçili gün ve [başlangıç, bitiş) aralığı — başlangıç dahil, bitiş hariç.
/// </summary>
public sealed class QuietHours
{
    public const int StepMinutes = 15;

    private QuietHours(Guid id, Guid tenantId, Guid teacherUserId, TimeOnly start, TimeOnly end, WorkDays days, DateTimeOffset updatedAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.TeacherUserId = teacherUserId;
        this.Start = start;
        this.End = end;
        this.Days = days;
        this.UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public TimeOnly Start { get; private set; }

    public TimeOnly End { get; private set; }

    public WorkDays Days { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static QuietHours Define(Guid tenantId, Guid teacherUserId, TimeOnly start, TimeOnly end, WorkDays days, DateTimeOffset now)
    {
        Validate(start, end, days);
        return new(Guid.CreateVersion7(now), tenantId, teacherUserId, start, end, days, now);
    }

    /// <summary>Saat geçerli mi: 15 dakikalık adımda, saniyesiz.</summary>
    public static bool IsOnStep(TimeOnly time) => time.Second == 0 && time.Millisecond == 0 && time.Minute % StepMinutes == 0;

    /// <summary>Haftanın gününün bayrak karşılığı.</summary>
    public static WorkDays DayFlag(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => WorkDays.Monday,
        DayOfWeek.Tuesday => WorkDays.Tuesday,
        DayOfWeek.Wednesday => WorkDays.Wednesday,
        DayOfWeek.Thursday => WorkDays.Thursday,
        DayOfWeek.Friday => WorkDays.Friday,
        DayOfWeek.Saturday => WorkDays.Saturday,
        _ => WorkDays.Sunday,
    };

    public void Change(TimeOnly start, TimeOnly end, WorkDays days, DateTimeOffset now)
    {
        Validate(start, end, days);
        this.Start = start;
        this.End = end;
        this.Days = days;
        this.UpdatedAt = now;
    }

    /// <summary>Türkiye saatiyle verilen gün ve saat mesai içinde mi (seçili gün; başlangıç dahil, bitiş hariç).</summary>
    public bool IsWorkingTime(DayOfWeek day, TimeOnly time) =>
        (this.Days & DayFlag(day)) != WorkDays.None && time >= this.Start && time < this.End;

    private static void Validate(TimeOnly start, TimeOnly end, WorkDays days)
    {
        if (!IsOnStep(start) || !IsOnStep(end) || start >= end || days == WorkDays.None)
        {
            throw new ArgumentException("Mesai saatleri geçersiz.", nameof(start));
        }
    }
}
