using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Bildirim komutunun sonucu: kayıt kalıcı olduktan <b>sonra</b> kuyruğa bırakılacak e-postalar, bildirim giden veli sayısı
/// ve velisi kayıtlı olmayan devamsız öğrenci sayısı.
/// </summary>
public sealed record AttendanceNoticeResult(IReadOnlyList<EmailMessage> Emails, int NotifiedGuardians, int StudentsWithoutGuardian);
