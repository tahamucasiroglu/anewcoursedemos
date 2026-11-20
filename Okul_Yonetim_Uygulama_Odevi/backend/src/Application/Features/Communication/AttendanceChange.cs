namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Yoklamada "yok" durumu değişen öğrenci (spec 0010): <c>IsAbsent</c> doğruysa yeni devamsızlık, yanlışsa "Yok"tan
/// "Var"/"Geç"e düzeltme. Ad yalnızca bildirim metni için taşınır, loglanmaz.
/// </summary>
public sealed record AttendanceChange(Guid StudentId, string StudentName, bool IsAbsent);
