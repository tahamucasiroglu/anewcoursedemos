using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>
/// Olay kaydı servisi: olayları repository üzerinden kalıcı hale getirir.
/// </summary>
public sealed class SystemEventService : SystemEventServiceBase
{
    private readonly ISystemEventRepository _repository;

    /// <summary>Initializes a new instance of the <see cref="SystemEventService"/> class.</summary>
    /// <param name="repository">Olay repository'si.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    public SystemEventService(ISystemEventRepository repository, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    protected override Task PersistAsync(SystemEvent systemEvent, CancellationToken cancellationToken) =>
        _repository.AddAsync(systemEvent, cancellationToken);

    /// <inheritdoc />
    protected override Task<SystemEvent?> LoadLatestAsync(CancellationToken cancellationToken) =>
        _repository.GetLatestAsync(cancellationToken);
}
