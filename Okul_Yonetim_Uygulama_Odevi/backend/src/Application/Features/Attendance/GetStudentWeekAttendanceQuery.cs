using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Öğrenci bazlı yoklama okuması (spec 0014 gereksinim 2; dayanak): verilen öğrencinin günü aralıkta (uçlar dahil) olan bütün
/// yoklama kayıtlarının sayıları ve yok/geç günleri — öğrenci hafta içinde sınıf değiştirdiyse eski sınıfınınkiler de. <b>Uç
/// noktası yoktur ve çağıranın bu öğrenciye erişimini denetlemez</b>: yalnızca kurum + şube filtresi uygulanır, okul ve şube
/// görünürlüğü olan bir oturum ister (oturumsuz çağrı boş sonuç alır). Erişim denetimi çağırandadır (spec 0014:
/// <c>WeeklySummaryGuard</c>; plan 0014 Risks 9). Öğrenci verisi taşıdığı için loglanmaz.
/// </summary>
public sealed record GetStudentWeekAttendanceQuery(Guid StudentId, DateOnly From, DateOnly To) : IRequest<StudentWeekAttendance>;
