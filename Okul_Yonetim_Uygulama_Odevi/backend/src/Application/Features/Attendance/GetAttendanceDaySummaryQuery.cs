using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Bugünün yoklama özeti (spec 0017 devamsızlık kartı); <c>BranchId</c> verilirse yalnızca o şube. Uç yoktur ve çağıranın erişimini
/// denetlemez (yalnızca kurum + şube filtresi) — erişim denetimi çağırandadır (yönetici paneli kapısı).
/// </summary>
public sealed record GetAttendanceDaySummaryQuery(Guid? BranchId) : IRequest<AttendanceDaySummary>;
