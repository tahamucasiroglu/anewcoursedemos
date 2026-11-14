using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajan → API bağlantısı: kendiliğinden bağlanır, koparsa kendiliğinden yeniden bağlanır ve her
/// bağlanmada kendini yeniden kaydeder (spec 0001 Req 7, AC-6).
/// </summary>
public interface IServerConnection : IAsyncDisposable
{
    /// <summary>Durum değiştiğinde tetiklenir.</summary>
    event EventHandler<ConnectionState>? StateChanged;

    /// <summary>Gets anlık bağlantı durumu.</summary>
    ConnectionState State { get; }

    /// <summary>Bağlantıyı başlatır; sunucu kapalıysa açılana kadar arka planda dener.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev (ilk bağlantıyı beklemez).</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>Bağlantıyı kapatır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
