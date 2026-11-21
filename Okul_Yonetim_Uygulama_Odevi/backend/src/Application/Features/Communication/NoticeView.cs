namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Velinin bildirim listesindeki satır (spec 0010 gereksinim 8): öğrenci, gün, tür (<c>Absent</c> / <c>Corrected</c> / spec 0013 <c>Assignment</c> —
/// "düzeltildi" etiketi), metin, okundu mu ve son değişiklik zamanı. Yalnızca öğrenci adı, gün, okul adı ve durum içerir.
/// Spec 0014: tür <c>WeeklySummary</c> — başlık ("Haftalık gelişim özeti — 16–22 Kasım", günden — haftanın Pazartesi'si — kurulur;
/// diğer türlerde boş) ve metin = onaylanan özet + etiket + dayanak satırı.
/// </summary>
public sealed record NoticeView(Guid Id, Guid StudentId, string StudentName, DateOnly Day, string Kind, string Text, bool IsRead, DateTimeOffset At, string? Title = null);
