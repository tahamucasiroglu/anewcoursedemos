using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;

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

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestVisibleAsync(Guid? userId, CancellationToken cancellationToken) =>
        Task.FromException<SystemEvent?>(new InvalidOperationException("veritabanı kapalı"));

    /// <inheritdoc />
    public Task<SystemEvent?> GetLatestOwnedAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromException<SystemEvent?>(new InvalidOperationException("veritabanı kapalı"));

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemEvent>> ListOwnedAsync(
        Guid userId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        IReadOnlyList<EventLevel>? levels,
        int skip,
        int take,
        CancellationToken cancellationToken) =>
        Task.FromException<IReadOnlyList<SystemEvent>>(new InvalidOperationException("veritabanı kapalı"));

    /// <inheritdoc />
    public Task<DateTimeOffset?> GetEarliestOwnedAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromException<DateTimeOffset?>(new InvalidOperationException("veritabanı kapalı"));
}
