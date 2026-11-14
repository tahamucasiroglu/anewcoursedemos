namespace TahaKonsept.Agent.Core.Hub.Abstract;

/// <summary>
/// Sunucu SignalR hub bağlantısı (<c>/hubs/agent</c>): otomatik yeniden bağlanır, her bağlantıda <c>Hello</c> gönderir,
/// sunucu komutlarını (<c>IAgentClient</c>) ajan servislerine iletir ve <see cref="IAgentReporter"/> olarak rapor taşır.
/// </summary>
public interface IAgentHubConnection : IAgentReporter
{
    /// <summary>Bağlantı durumu.</summary>
    HubConnectionStatus Status { get; }

    /// <summary>Durum değişince tetiklenir.</summary>
    event EventHandler<HubConnectionStatus>? StatusChanged;

    /// <summary>Bağlantı döngüsünü başlatır (hemen döner; eşleme yoksa eşleme beklenir).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StartAsync(CancellationToken ct);

    /// <summary>Bağlantıyı kapatır.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken ct);

    /// <summary>Güncel donanım/bileşen bilgisiyle <c>Hello</c> gönderir.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SendHelloAsync(CancellationToken ct);
}
