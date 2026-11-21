using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Velinin "Randevularım"ı (spec 0016 gereksinim 8): bütün çocuklarının ve kendi talep ettiği randevular — yaklaşan ve son 30 gün. Adlar taşıdığı için loglanmaz (T9).</summary>
public sealed record ListMyAppointmentsQuery : IRequest<MyAppointments>;
