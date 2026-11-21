using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Çocuğun öğretmeninin başlamamış boş dilimleri (spec 0016 gereksinim 3); çocuk velinin değilse ya da öğretmen çocuğun sınıfının öğretmeni değilse 404.</summary>
public sealed record ListOpenSlotsQuery(Guid TeacherUserId, Guid StudentId) : IRequest<IReadOnlyList<OpenSlot>>;
