namespace OkulYonetim.Application.Ports;

/// <summary>
/// Gönderilecek tek bildirim (spec 0011): okul, alıcı kişi, tür ve kaynak kayıt (duyuru, mesaj, veli bildirimi) kimliği.
/// Metin ve ad taşımaz (gizlilik, gereksinim 8); uygulama içeriği kaynağından okur.
/// </summary>
public sealed record PushNotification(Guid TenantId, Guid RecipientUserId, PushNotificationKind Kind, Guid SourceId);
