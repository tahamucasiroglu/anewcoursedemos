using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Mapping;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IJobQueryService"/> ortak davranışı: oturumdaki kullanıcının işini çalıştırmalarıyla döner.
/// </summary>
public abstract class JobQueryServiceBase : IJobQueryService
{
    private readonly IJobRepository _jobs;
    private readonly ICurrentUser _currentUser;

    /// <summary>Oluşturur.</summary>
    /// <param name="jobs">İş deposu.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    protected JobQueryServiceBase(IJobRepository jobs, ICurrentUser currentUser)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>İş ayrıntısı.</summary>
    /// <param name="id">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayrıntı.</returns>
    public async Task<Result<JobDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await _jobs.GetForUserNoTrackingAsync(id, _currentUser.GetRequiredUserId(), cancellationToken);
        return job is null ? ProductionErrors.JobNotFound : job.ToDetailDto();
    }
}
