using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>
/// Velinin randevu talebi (spec 0016 gereksinim 3–4): dilim, çocuk ve konu (zorunlu, en fazla 300). Konu taşıdığı için bütünüyle
/// loglanmaz (T9); işleyici yalnızca kimlikleri loglar.
/// </summary>
public sealed record RequestAppointmentCommand(Guid? SlotId, Guid? StudentId, string? Topic) : IRequest<AppointmentChanged>;
