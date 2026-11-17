using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajanın ilk kurulumu (spec 0009): bileşen listesi ve durumları, ön kontrol, sırayla indirme → doğrulama → kurulum, otomatik
/// yeniden deneme ve Pi aynası, Tekrar dene / Onar, kesintiden devam, bütünlük denetimi ve Hazır denetimi.
/// </summary>
public interface ISetupCoordinator
{
    /// <summary>Kurulum olayı (arka plan iş parçacığından).</summary>
    event EventHandler<SetupEvent>? Changed;

    /// <summary>Gets bileşenlerin anlık durumları (liste sırası kurulum sırasıdır).</summary>
    IReadOnlyList<ComponentState> Components { get; }

    /// <summary>Gets kurulum yeri (<c>%LOCALAPPDATA%\anewconcept</c>).</summary>
    string InstallRoot { get; }

    /// <summary>Gets tespit edilen ekran kartı (spec 0028 Req 5) ya da <c>null</c>.</summary>
    GpuInfo? Gpu { get; }

    /// <summary>Gets video klasörü (seçilmediyse varsayılan).</summary>
    string VideoFolder { get; }

    /// <summary>Gets a value indicating whether kurulum sürüyor.</summary>
    bool IsRunning { get; }

    /// <summary>Gets a value indicating whether tüm bileşenler kurulu ve sağlam (Hazır).</summary>
    bool IsReady { get; }

    /// <summary>Gets toplam ilerleme yüzdesi (tray ipucu).</summary>
    int Percent { get; }

    /// <summary>Açılışta: yarım kalanları temizler, kurulu bileşenlerin bütünlüğünü denetler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kurulum ekranı gerekiyorsa <c>true</c>.</returns>
    Task<bool> InspectAsync(CancellationToken cancellationToken);

    /// <summary>Ön kontrol: internet ve gereken/boş alan.</summary>
    /// <returns>Sonuç.</returns>
    SetupPreflight Preflight();

    /// <summary>Video klasörünü değiştirir.</summary>
    /// <param name="folder">Klasör.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    bool TrySetVideoFolder(string folder);

    /// <summary>Kurulmamış, hatalı ya da onarılması gereken tüm bileşenleri kurar ve Hazır denetimini çalıştırır.</summary>
    /// <param name="cancellationToken">İptal belirteci (tray "Çıkış").</param>
    /// <returns>Görev.</returns>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>Tekrar dene / Onar: yalnızca bu bileşeni yeniden kurar.</summary>
    /// <param name="componentId">Bileşen kimliği.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task RetryAsync(string componentId, CancellationToken cancellationToken);

    /// <summary>
    /// Üretimin istediği bileşenleri kurar (spec 0027 Req 5, 8): yalnızca listedeki ve kurulu olmayan kimlikler (isteğe bağlı olanlar
    /// dahil); bitince tüm bileşenler hazırsa Hazır denetimi çalışır. Kurulum zaten sürüyorsa hiçbir şey yapmaz ve <c>false</c> döner (çağıran bekleyip yeniden dener).
    /// </summary>
    /// <param name="componentIds">Bileşen kimlikleri.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kurulum bu çağrıda çalıştıysa <c>true</c>.</returns>
    Task<bool> InstallAsync(IReadOnlyCollection<string> componentIds, CancellationToken cancellationToken);
}
