using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Üretim işleri deposu (çalıştırmalarıyla birlikte yüklenir).
/// </summary>
public interface IJobRepository : IRepository<Job>
{
    /// <summary>İş + çalıştırmalar (takipli).</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Job?> GetWithRunsAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Kullanıcının işi + çalıştırmalar (takipsiz).</summary>
    /// <param name="id">İş.</param>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Job?> GetForUserNoTrackingAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    /// <summary>Ajan komut kimliğine göre iş + çalıştırmalar (takipli).</summary>
    /// <param name="commandId">Komut.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<Job?> FindByCommandAsync(Guid commandId, CancellationToken cancellationToken);

    /// <summary>İşin çalıştırmaları (takipsiz).</summary>
    /// <param name="jobId">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<StageRun>> ListRunsNoTrackingAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>Birden çok iş + çalıştırmalar (takipsiz).</summary>
    /// <param name="ids">İşler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<IReadOnlyList<Job>> ListWithRunsNoTrackingAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
}
