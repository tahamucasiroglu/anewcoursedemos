using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>Sınıfın bugünkü yoklama listesi (spec 0010 gereksinim 3). Yalnızca sınıfa atanmış öğretmen; kimlik dışında veri taşımaz.</summary>
public sealed record GetTodayAttendanceQuery(Guid ClassroomId) : IRequest<AttendanceSheetView>, ILoggableRequest;
