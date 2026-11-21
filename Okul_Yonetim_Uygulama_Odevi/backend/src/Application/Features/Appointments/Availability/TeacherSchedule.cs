namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>
/// "Takvimim" (spec 0016 gereksinim 2): yanıt bekleyen talep sayısı, üstte gösterilen yanıt bekleyen talepler (en yakın önce) ve
/// bugünden (Türkiye günü) itibaren bütün dilimler gün ve saat sırasıyla.
/// </summary>
public sealed record TeacherSchedule(int PendingCount, IReadOnlyList<ScheduleItem> Pending, IReadOnlyList<ScheduleItem> Slots);
