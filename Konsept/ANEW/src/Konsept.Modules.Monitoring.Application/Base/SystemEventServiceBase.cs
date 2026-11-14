using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;

namespace Konsept.Modules.Monitoring.Application.Base;

/// <summary>
/// Olay servisinin ortak mantığı: zamanı <see cref="TimeProvider"/>'dan alır, domain ile Contracts
/// arasında dönüştürür; kalıcılığı alt sınıfa bırakır.
/// </summary>
public abstract class SystemEventServiceBase : ISystemEventService
{
    /// <summary>Initializes a new instance of the <see cref="SystemEventServiceBase"/> class.</summary>
    /// <param name="timeProvider">Saat kaynağı.</param>
    protected SystemEventServiceBase(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task<SystemEventDto> WriteAsync(SystemEventEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var systemEvent = SystemEvent.Create(
            entry.Type,
            (EventLevel)entry.Level,
            entry.Message,
            TimeProvider.GetUtcNow(),
            entry.UserId);
        OnEventCreated(systemEvent);
        await PersistAsync(systemEvent, cancellationToken).ConfigureAwait(false);
        return ToDto(systemEvent);
    }

    /// <inheritdoc />
    public async Task<SystemEventDto?> GetLatestAsync(CancellationToken cancellationToken)
    {
        var latest = await LoadLatestAsync(cancellationToken).ConfigureAwait(false);
        return latest is null ? null : ToDto(latest);
    }

    /// <summary>
    /// Olay oluşturulunca, kalıcı yapılmadan önce çağrılır: olay teknik loga da yazılır (spec 0008 Req 3) —
    /// veritabanı kapalı olsa bile dosya logunda bulunsun diye önce.
    /// </summary>
    /// <param name="systemEvent">Olay (metni maskelenmiş).</param>
    protected abstract void OnEventCreated(SystemEvent systemEvent);

    /// <summary>Olayı kalıcı olarak kaydeder.</summary>
    /// <param name="systemEvent">Olay.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task PersistAsync(SystemEvent systemEvent, CancellationToken cancellationToken);

    /// <summary>En son olayı yükler.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Olay ya da <c>null</c>.</returns>
    protected abstract Task<SystemEvent?> LoadLatestAsync(CancellationToken cancellationToken);

    private static SystemEventDto ToDto(SystemEvent e) =>
        new(e.Id, e.Type, (SystemEventLevel)e.Level, e.Message, e.OccurredAt, e.UserId);
}
