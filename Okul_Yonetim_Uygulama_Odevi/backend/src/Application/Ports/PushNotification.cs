namespace OkulYonetim.Application.Ports;

/// <summary>
/// Gönderilecek tek bildirim (spec 0011): okul, alıcı kişi, tür ve kaynak kayıt (duyuru, mesaj, veli bildirimi) kimliği.
/// Ad taşımaz (gizlilik, gereksinim 8); uygulama içeriği kaynağından okur. Spec 0016: öğretmene giden randevu bildiriminin
/// kaynağı veli tarafında bir kayıt olmadığı için kısa metni (<see cref="Text"/>: yalnızca tür, gün ve saat — ad ve konu yok) taşır;
/// sahte gönderici metni de loglamaz.
/// </summary>
public sealed record PushNotification(Guid TenantId, Guid RecipientUserId, PushNotificationKind Kind, Guid SourceId, string? Text = null);
