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

    /// <summary>
    /// Sunucu ajanın oturumunu kapattığında tetiklenir (spec 0012 Req 6, 12): "oturum kapandı" bildirimi geldi ya da belirteç
    /// reddedildi (401). Bağlantı bu durumda kendiliğinden yeniden denemez.
    /// </summary>
    event EventHandler? SessionRevoked;

    /// <summary>
    /// Sunucu kayıtta ajanın sürümünün kendisininkinden farklı olduğunu bildirdi (spec 0023 Req 4); argüman sunucu sürümü. Ajan bu durumda hiçbir
    /// komut yürütmez ve güncelleme ekranını açar.
    /// </summary>
    event EventHandler<string>? UpdateRequiredReceived;

    /// <summary>Gets a value indicating whether sürüm uyumsuzluğu var (komutlar yürütülmez).</summary>
    bool UpdateRequired { get; }

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

    /// <summary>Bağlıysa ajanın eşleştiği hesabı sorar (spec 0022 Req 8); bağlı değilse ya da hata olursa <c>null</c>.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Hesap kimliği ya da <c>null</c>.</returns>
    Task<Guid?> TryGetAccountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Bağlıysa hata bildirimini gönderir (spec 0022 Req 4); bağlı değilse ya da iletilemezse <c>null</c> (çağıran bekletir). Hata fırlatmaz.
    /// </summary>
    /// <param name="report">Rapor (hesabı doldurulmuş).</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç ya da <c>null</c>.</returns>
    Task<ReportOutcome?> TrySendErrorReportAsync(AgentErrorReport report, CancellationToken cancellationToken);

    /// <summary>
    /// Sunucuya güncellemeye başlamak istediğini bildirir (spec 0023 Req 3): kullanıcının "Üretiliyor" işi varsa <c>false</c>; başlayabilirse
    /// <c>true</c> (sunucu güncelleme bitene kadar ajana iş göndermez); bağlı değilse <c>null</c> (iş zaten gönderilemez).
    /// </summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<bool?> TryBeginUpdateAsync(CancellationToken cancellationToken);

    /// <summary>Güncelleme kurulmadan bitti: sunucu ajana yeniden iş gönderebilir (review r1 H-1). Hata yutulur.</summary>
    /// <returns>Görev.</returns>
    Task TryEndUpdateAsync();

    /// <summary>Bağlantıyı yeniden başlatılabilir biçimde kapatır (oturum kapandı; yeni eşleştirmeden sonra <see cref="StartAsync"/>).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task SuspendAsync(CancellationToken cancellationToken);

    /// <summary>Bağlantıyı kapatır.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
