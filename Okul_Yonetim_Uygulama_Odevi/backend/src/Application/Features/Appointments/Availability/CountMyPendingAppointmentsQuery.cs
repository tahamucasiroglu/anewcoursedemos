using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>Öğretmenin yanıt bekleyen randevu talebi sayısı (spec 0016: hoş geldin'deki "Randevular (n bekleyen)"; <c>/api/me</c>). Öğretmen değilse 0.</summary>
public sealed record CountMyPendingAppointmentsQuery : IRequest<int>;
