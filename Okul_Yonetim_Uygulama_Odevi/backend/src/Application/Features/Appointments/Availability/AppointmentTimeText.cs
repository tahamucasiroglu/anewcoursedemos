using System.Globalization;
using OkulYonetim.Domain.Appointments;

namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>Müsaitlik saatlerinin API biçimi (spec 0016): "SS:DD" (24 saat, değişmez kültür, 15 dakikalık adım; 0011 mesai saati kalıbı).</summary>
public static class AppointmentTimeText
{
    public static bool TryParse(string? text, out TimeOnly time) =>
        TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time) && AvailabilityPlanner.IsOnStep(time);
}
