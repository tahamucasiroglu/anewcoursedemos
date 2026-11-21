namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>
/// Velinin randevu satırı (spec 0016 gereksinim 8): öğretmen, çocuk, başlangıç/bitiş (UTC), durum (<c>Pending</c> / <c>Approved</c> /
/// <c>Rejected</c> / <c>CancelledByGuardian</c> / <c>CancelledByTeacher</c> / <c>Expired</c>), ret/iptal nedeni, talebi bu veli mi yaptı,
/// talep edenin adı ve iptal düğmesi (yalnızca talep eden velide, açık randevuda). Konu metni bu listede yoktur.
/// </summary>
public sealed record MyAppointmentRow(
    Guid Id,
    Guid TeacherUserId,
    string TeacherName,
    Guid StudentId,
    string StudentName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string State,
    string? Reason,
    bool RequestedByMe,
    string RequestedByName,
    bool CanCancel);
