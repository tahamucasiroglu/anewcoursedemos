using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Bugünün yoklamasını tek seferde kaydeder ya da düzeltir (spec 0010 gereksinim 4–7). Gün yalnızca doğrulama için gelir;
/// sunucu kendi Türkiye gününü kullanır, farklıysa reddeder. Devam bilgisi taşıdığı için bütünüyle loglanmaz (T9); işleyici
/// yalnızca kimlik ve sayıları loglar.
/// </summary>
public sealed record SaveTodayAttendanceCommand(Guid ClassroomId, DateOnly? Day, IReadOnlyList<AttendanceEntryInput>? Entries) : IRequest<AttendanceSaveResult>;
