namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>"Randevularım" (spec 0016 gereksinim 8): yaklaşan randevular (en yakın üstte) ve son 30 günün geçmiş randevuları (en yeni üstte).</summary>
public sealed record MyAppointments(IReadOnlyList<MyAppointmentRow> Upcoming, IReadOnlyList<MyAppointmentRow> Past);
