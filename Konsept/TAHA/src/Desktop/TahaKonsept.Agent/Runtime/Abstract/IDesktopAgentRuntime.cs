namespace TahaKonsept.Agent.Runtime.Abstract;

/// <summary>
/// Masaüstü uygulamasının yaşam döngüsü: <see cref="StartAsync"/> ajan çekirdeğini (<c>IAgentHost</c>) başlatır, ilk açılışta
/// "Windows ile başlat"ı açar, gerekiyorsa kurulum penceresini gösterir ve güncellemeyi arka planda denetler;
/// <see cref="StopAsync"/> çekirdeği durdurur ve indirilen güncellemeyi kapanışta kurulacak şekilde ayarlar.
/// </summary>
public interface IDesktopAgentRuntime
{
    /// <summary>Başlatır (UI iş parçacığında çağrılır; uzun işler arka planda).</summary>
    /// <param name="options">Başlatma seçenekleri.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StartAsync(DesktopLaunchOptions options, CancellationToken ct);

    /// <summary>Durdurur (çıkışta).</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken ct);
}
