using System.Collections.Concurrent;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>Bildirim kapısından çıkan bildirimleri saklar (sahte göndericinin yerine; spec 0011 AC-9, AC-10).</summary>
public sealed class CapturingPushNotifier : IPushNotifier
{
    public ConcurrentQueue<PushNotification> Sent { get; } = new();

    public PushNotification[] To(Guid userId) => [.. this.Sent.Where(p => p.RecipientUserId == userId)];

    public Task NotifyAsync(PushNotification notification, CancellationToken cancellationToken)
    {
        this.Sent.Enqueue(notification);
        return Task.CompletedTask;
    }
}
