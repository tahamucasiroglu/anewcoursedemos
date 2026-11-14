namespace TahaKonsept.Agent.Updates.Abstract;

/// <summary>
/// Uygulama güncellemesi (Velopack): <see cref="CheckAndDownloadAsync"/> web sunucusundaki <c>downloads/</c> klasöründe yeni sürüm arar
/// ve indirir; <see cref="ApplyOnExit"/> uygulama kapanırken indirilen sürümü sessizce kurar. Kurulu değilse (geliştirme) hiçbir şey yapmaz.
/// </summary>
public interface IAgentUpdater
{
    /// <summary>Yeni sürüm indirildi ve kapanışta kurulacak mı.</summary>
    bool IsUpdatePending { get; }

    /// <summary>Yeni sürümü denetler ve indirir.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yeni sürüm indirildiyse <c>true</c>.</returns>
    Task<bool> CheckAndDownloadAsync(CancellationToken ct);

    /// <summary>İndirilen sürüm varsa uygulama kapandıktan sonra kurulmasını ayarlar (yeniden başlatmadan).</summary>
    void ApplyOnExit();
}
