using OkulYonetim.Application.Ports;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>Her bildirimde hata veren gönderici (spec 0011 AC-10: gönderici hatası asıl işlemi bozmaz, Warning loglanır).</summary>
public sealed class ThrowingPushNotifier : IPushNotifier
{
    public int Attempts { get; private set; }

    public Task NotifyAsync(PushNotification notification, CancellationToken cancellationToken)
    {
        this.Attempts++;
        throw new HttpRequestException("push servisi yanıt vermedi");
    }
}
