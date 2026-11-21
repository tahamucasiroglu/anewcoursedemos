using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Appointments.Availability;

/// <summary>Öğretmenin "Takvimim"i (spec 0016 gereksinim 2). Veli ve çocuk adı ile konu taşıdığı için loglanmaz (T9).</summary>
public sealed record GetMyScheduleQuery : IRequest<TeacherSchedule>;
