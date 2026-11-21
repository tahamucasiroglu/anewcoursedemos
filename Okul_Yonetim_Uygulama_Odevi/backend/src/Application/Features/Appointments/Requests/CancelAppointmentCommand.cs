using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>
/// Randevu iptali (spec 0016 gereksinim 6): talep eden veli açık randevusunu, dilimin öğretmeni onaylı randevuyu başlamadan iptal eder;
/// öğretmen isteğe bağlı neden yazar (en fazla 300). Neden taşıdığı için loglanmaz (T9).
/// </summary>
public sealed record CancelAppointmentCommand(Guid AppointmentId, string? Reason) : IRequest<AppointmentChanged>;
