using Konsept.Modules.Identity.Contracts.Abstract;
using Konsept.Modules.Production.Application.Abstract;

namespace Konsept.Modules.Production.Application.Base;

/// <summary>
/// Kullanıcının son web oturumu kapanınca (çıkış; spec 0006 Req 16, 21) çalışan işi <i>Durduruldu</i> yapar: sıra durur, <i>Sırada</i> işler
/// kendiliğinden başlamaz; yeniden girişte "Kaldığı yerden devam et" ile sürer.
/// </summary>
/// <param name="jobs">İş deposu.</param>
/// <param name="time">Saat.</param>
public abstract class ProductionSessionListenerBase(IProductionJobRepository jobs, TimeProvider time) : IUserSessionsEndedListener
{
    /// <inheritdoc />
    public Task OnAllSessionsEndedAsync(Guid userId, CancellationToken cancellationToken) =>
        jobs.StopAsync(userId, null, time.GetUtcNow(), cancellationToken);
}
