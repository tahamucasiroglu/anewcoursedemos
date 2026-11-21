using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Dashboard.Summary;

/// <summary>Günlük özetin devamsızlık kartı (spec 0017 gereksinim 3); <c>BranchId</c> boşsa bütün şubeler. Yalnızca kurucu.</summary>
public sealed record GetAttendanceCardQuery(Guid? BranchId) : IRequest<AttendanceCard>;
