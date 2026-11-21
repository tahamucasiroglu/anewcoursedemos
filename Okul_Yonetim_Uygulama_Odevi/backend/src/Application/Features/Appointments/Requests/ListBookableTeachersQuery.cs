using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Requests;

/// <summary>Velinin çocuğu için randevu alabileceği öğretmenler (spec 0016 gereksinim 3); çocuk velinin değilse 404.</summary>
public sealed record ListBookableTeachersQuery(Guid StudentId) : IRequest<IReadOnlyList<BookableTeacher>>;
