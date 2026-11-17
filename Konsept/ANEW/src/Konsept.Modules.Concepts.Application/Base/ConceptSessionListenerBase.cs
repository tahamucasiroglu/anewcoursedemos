using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Domain.Enums;
using Konsept.Modules.Identity.Contracts.Abstract;

namespace Konsept.Modules.Concepts.Application.Base;

/// <summary>
/// Kullanıcının tüm oturumları kapanınca (çıkış — spec 0014 Req 20 —, ban — spec 0024 Req 4) süren konu önerisi / konsept üretim
/// partilerini Req 19'daki gibi durdurur: süren çağrı tamamlanır, başlamamış konseptler için çağrı yapılmaz.
/// </summary>
/// <param name="registry">İş kaydı.</param>
public abstract class ConceptSessionListenerBase(IConceptJobRegistry registry) : IUserSessionsEndedListener
{
    /// <inheritdoc />
    public Task OnAllSessionsEndedAsync(Guid userId, CancellationToken cancellationToken)
    {
        foreach (var lane in Enum.GetValues<JobLane>())
        {
            if (registry.Get(userId, lane) is { IsRunning: true } job)
            {
                job.RequestStop();
            }
        }

        return Task.CompletedTask;
    }
}
