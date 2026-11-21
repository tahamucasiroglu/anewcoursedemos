using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Günü aralıkta (uçlar dahil) olan yoklamalardan öğrenci başına yok ve geç gün sayısı (spec 0017 gereksinim 7); şube ve sınıf yoklamanın
/// alındığı şube/sınıftır; "en az <c>MinAbsent</c> gün yok" eşiği; hiç yok/geç kaydı olmayan öğrenci listede yok. Sıra: yok sayısına göre
/// azalan, eşitlikte Türkçe ad. Uç yoktur ve erişim denetlemez — çağıran denetler.
/// </summary>
public sealed record ListAttendanceTotalsQuery(DateOnly FromDay, DateOnly ToDay, Guid? BranchId, Guid? ClassroomId, int MinAbsent) : IRequest<IReadOnlyList<StudentAttendanceTotal>>;
