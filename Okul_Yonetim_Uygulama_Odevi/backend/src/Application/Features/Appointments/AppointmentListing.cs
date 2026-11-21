using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Features.Appointments;

/// <summary>
/// Randevunun liste satırı (spec 0016; depo projeksiyonu, T7): kimlikler, öğretmen/veli/çocuk adları (yalnızca ekranda gösterilir,
/// loglanmaz), dilimin başlangıç ve bitişi, konu, saklanan durum ve neden.
/// </summary>
public sealed record AppointmentListing(
    Guid Id,
    Guid SlotId,
    Guid TeacherUserId,
    string TeacherName,
    Guid GuardianUserId,
    string GuardianName,
    Guid StudentId,
    string StudentName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Topic,
    AppointmentStatus Status,
    string? Reason);
