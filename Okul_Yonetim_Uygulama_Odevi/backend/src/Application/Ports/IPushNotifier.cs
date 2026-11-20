namespace OkulYonetim.Application.Ports;

/// <summary>
/// Kişiye bildirim gönderen dış servis portu (spec 0011 C-7). MVP'de gerçek push yok (B-6): Infrastructure'daki sahte
/// gönderici yalnızca kimlikleri loglar. Yalnızca Communication'ın bildirim kapısı (<c>GuardianNotifier</c>) çağırır; hata
/// verirse kapı Warning loglar, asıl işlem başarılı kalır.
/// </summary>
public interface IPushNotifier
{
    Task NotifyAsync(PushNotification notification, CancellationToken cancellationToken);
}
