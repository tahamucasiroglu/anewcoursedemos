using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Bildirim komutunun sonucu: kayıt kalıcı olduktan <b>sonra</b> bildirim kapısına (<c>DeliverNotificationsCommand</c>, spec 0011)
/// verilecek bildirimler ve e-postalar, bildirim giden veli sayısı ve velisi kayıtlı olmayan devamsız öğrenci sayısı.
/// </summary>
public sealed record AttendanceNoticeResult(IReadOnlyList<EmailMessage> Emails, IReadOnlyList<PushNotification> Pushes, int NotifiedGuardians, int StudentsWithoutGuardian);
