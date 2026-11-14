using Konsept.Agent.Domain.Entities;
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

    /// <summary>
    /// Bağlıysa Error/Critical log özetini API'ye gönderir (spec 0008 Req 7); bağlı değilse göndermez ve sonradan da
    /// göndermez. Hata fırlatmaz.
    /// </summary>
    /// <param name="summary">Özet.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>API kabul ettiyse <c>true</c>.</returns>
    Task<bool> TryReportLogAsync(LogSummary summary, CancellationToken cancellationToken);

    /// <summary>Bağlantıyı kapatır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
