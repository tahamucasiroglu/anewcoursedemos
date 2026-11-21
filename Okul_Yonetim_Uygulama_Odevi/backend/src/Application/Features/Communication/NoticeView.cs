namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Velinin bildirim listesindeki satır (spec 0010 gereksinim 8): öğrenci, gün, tür (<c>Absent</c> / <c>Corrected</c> / spec 0013 <c>Assignment</c> —
/// "düzeltildi" etiketi), metin, okundu mu ve son değişiklik zamanı. Yalnızca öğrenci adı, gün, okul adı ve durum içerir.
/// </summary>
public sealed record NoticeView(Guid Id, Guid StudentId, string StudentName, DateOnly Day, string Kind, string Text, bool IsRead, DateTimeOffset At);
