namespace OkulYonetim.Application.Features.Attendance;

/// <summary>
/// Kaydetme sonucu (spec 0010 gereksinim 4): var/yok/geç sayıları, bu kayıtla bildirim giden veli sayısı, velisi kayıtlı
/// olmayan devamsız öğrenci sayısı ve kayıt saati (Türkiye saatiyle <c>SS:DD</c>). Metni istemci kurar.
/// </summary>
public sealed record AttendanceSaveResult(int Present, int Absent, int Late, int NotifiedGuardians, int StudentsWithoutGuardian, string SavedTime);
