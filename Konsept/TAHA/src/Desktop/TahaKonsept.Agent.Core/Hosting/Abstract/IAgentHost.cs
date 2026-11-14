using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.Pairing.Abstract;
using TahaKonsept.Agent.Core.Prerequisites.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Hosting.Abstract;

/// <summary>
/// Ajan çekirdeğinin tek giriş noktası (Avalonia uygulaması kullanır): <see cref="StartAsync"/> yerel video sunucusunu ve hub
/// bağlantı döngüsünü başlatır (eşleme yoksa eşleme beklenir), <see cref="StopAsync"/> işleri durdurup her şeyi kapatır.
/// UI için <see cref="Hub"/> (bağlantı durumu), <see cref="Pairing"/> (eşleme penceresi), <see cref="Prerequisites"/> (kurulum ekranı) açılır.
/// </summary>
public interface IAgentHost
{
    /// <summary>Çalışıyor mu.</summary>
    bool IsRunning { get; }

    /// <summary>Hub bağlantısı (durum + olay).</summary>
    IAgentHubConnection Hub { get; }

    /// <summary>Cihaz eşleme.</summary>
    IDevicePairingService Pairing { get; }

    /// <summary>Ön gereksinim kurulumu.</summary>
    IPrerequisiteInstaller Prerequisites { get; }

    /// <summary>Ajanı başlatır.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Sonuç (video sunucusu açılamazsa uyarı niteliğinde hata; hub yine başlar).</returns>
    Task<Result> StartAsync(CancellationToken ct);

    /// <summary>Ajanı durdurur.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken ct);
}
