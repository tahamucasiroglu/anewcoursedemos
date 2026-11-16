using Konsept.Agent.Domain.Entities;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajanın oturum akışı (spec 0012): eşleşmemişse kod alır ve onayı bekler, eşleşmişse sunucuya bağlanır; oturum kapanınca
/// belirteci siler ve yeni kod ister; sunucuya ulaşılamazsa yeniden dener.
/// </summary>
public interface IAgentSessionCoordinator
{
    /// <summary>Durum değiştiğinde tetiklenir (arka plan iş parçacığından).</summary>
    event EventHandler<AgentSessionSnapshot>? Changed;

    /// <summary>Gets anlık durum.</summary>
    AgentSessionSnapshot Current { get; }

    /// <summary>Akışı başlatır (ilk sonucu beklemez).</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>"Yeni kod al": süresi dolan ya da reddedilen kodun yerine yeni kod ister.</summary>
    void RequestNewCode();

    /// <summary>Akışı durdurur.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task StopAsync(CancellationToken cancellationToken);
}
