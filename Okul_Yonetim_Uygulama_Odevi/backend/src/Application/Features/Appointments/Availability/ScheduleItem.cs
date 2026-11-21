namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>
/// Öğretmenin takvimindeki dilim (spec 0016 gereksinim 2): dilim, başlangıç/bitiş (UTC), durum (<c>Free</c> boş, <c>Pending</c> onay
/// bekliyor, <c>Approved</c> onaylı, <c>Expired</c> süresi doldu), başlamış mı ve randevu varsa kimliği, veli adı, çocuk adı ve konu.
/// </summary>
public sealed record ScheduleItem(
    Guid SlotId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string State,
    bool HasStarted,
    Guid? AppointmentId,
    string? GuardianName,
    string? StudentName,
    string? Topic);
