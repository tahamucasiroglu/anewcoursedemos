using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>Kayda bağlı okul bildirimi komutunun sonucu (spec 0013): commit sonrası bildirim kapısına verilecek bildirimler ve bildirim giden veli sayısı.</summary>
public sealed record SchoolNoticeResult(IReadOnlyList<PushNotification> Pushes, int NotifiedGuardians);
