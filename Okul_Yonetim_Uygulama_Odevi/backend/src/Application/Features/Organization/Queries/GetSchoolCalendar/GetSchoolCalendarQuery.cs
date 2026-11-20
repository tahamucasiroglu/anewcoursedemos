using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.GetSchoolCalendar;

/// <summary>Seçili okulun takvimi: güncel (ya da yaklaşan) dönem ve kayıtları. Okulda rolü olan herkes.</summary>
public sealed record GetSchoolCalendarQuery : IRequest<SchoolCalendarView>, ILoggableRequest;
