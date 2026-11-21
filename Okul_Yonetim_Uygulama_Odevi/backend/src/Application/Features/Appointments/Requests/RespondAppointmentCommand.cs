using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>
/// Öğretmenin onay bekleyen talebi yanıtlaması (spec 0016 gereksinim 5): <c>Approve</c> ya da <c>Reject</c> ve isteğe bağlı neden
/// (en fazla 300; yalnızca retle saklanır). Neden taşıdığı için loglanmaz (T9).
/// </summary>
public sealed record RespondAppointmentCommand(Guid AppointmentId, string? Decision, string? Reason) : IRequest<AppointmentChanged>;
