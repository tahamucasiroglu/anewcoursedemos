using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajan güncellemesi (spec 0023): açılışta ve 6 saatte bir denetim, imza/sürüm doğrulaması, kullanıcı onayıyla güncelleme ("Üretiliyor" iş
/// varken bekler), SHA-256 doğrulaması, tek önceki sürüm, yeni sürümün ilk açılışında 60 sn içinde bağlanamazsa ya da yarım kalırsa geri dönüş.
/// </summary>
public interface IAgentUpdater
{
    /// <summary>Durum değişti (arka plan iş parçacığından).</summary>
    event EventHandler<UpdateEvent>? Changed;

    /// <summary>Zamanlanmış denetim yeni sürüm buldu (tray bildirimi "Yeni sürüm hazır: X").</summary>
    event EventHandler<UpdateManifest>? AvailableFound;

    /// <summary>Yeni ya da önceki sürüm başlatıldı; bu süreç kapanmalı.</summary>
    event EventHandler? RestartRequested;

    /// <summary>Gets kurulu sürüm.</summary>
    string CurrentVersion { get; }

    /// <summary>Gets hazır yeni sürüm; yoksa <c>null</c>.</summary>
    UpdateManifest? Available { get; }

    /// <summary>Gets son durum.</summary>
    UpdateEvent Current { get; }

    /// <summary>Hemen denetler (açılış, tray "Güncellemeleri denetle", zamanlayıcı).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<UpdateCheckOutcome> CheckAsync(CancellationToken cancellationToken);

    /// <summary>6 saatte bir denetler; yeni sürüm bulunca yalnızca haber verir (onaysız güncelleme yok).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task RunScheduleAsync(CancellationToken cancellationToken);

    /// <summary>
    /// "Şimdi güncelle": "Üretiliyor" iş varsa bekler ve iş bitince kendiliğinden başlar; indirir, SHA-256'yı doğrular, kurar, yeni sürümü başlatır.
    /// </summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Güncelleme tamamlandıysa <c>true</c>.</returns>
    Task<bool> ApplyAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Açılış: yarım kalan indirmeyi temizler; onay bekleyen yeni sürümse 60 sn içinde bağlanmasını bekler (bağlanırsa onaylar, bağlanamazsa geri
    /// döner), ikinci onaysız açılışta hemen geri döner; önceki sürümse geri dönüş bildirimini verir.
    /// </summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Açılış sonucu.</returns>
    Task<UpdateStartup> StartupAsync(CancellationToken cancellationToken);
}
