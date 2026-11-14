using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Veritabanı kapalıyken olay deposu: her yazma ve okuma hata verir.
/// </summary>
public sealed class FailingSystemEventRepository : ISystemEventRepository
{
    /// <inheritdoc />
    public Task AddAsync(SystemEvent systemEvent, CancellationToken cancellationToken) =>
        Task.FromException(new InvalidOperationException("veritabanı kapalı"));

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestAsync(CancellationToken cancellationToken) =>
        Task.FromException<SystemEvent?>(new InvalidOperationException("veritabanı kapalı"));
}
