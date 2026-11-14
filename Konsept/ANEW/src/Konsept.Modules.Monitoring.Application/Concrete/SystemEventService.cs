using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Application.Base;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Monitoring.Application.Concrete;

/// <summary>
/// Olay kaydı servisi: olayları repository üzerinden kalıcı hale getirir ve teknik loga eşlenmiş seviyede yazar
/// (spec 0008 Req 3: Bilgi → Information, Uyarı → Warning, Hata → Error, Kritik → Critical).
/// </summary>
public sealed partial class SystemEventService : SystemEventServiceBase
{
    private readonly ISystemEventRepository _repository;
    private readonly ILogger<SystemEventService> _logger;

    /// <summary>Initializes a new instance of the <see cref="SystemEventService"/> class.</summary>
    /// <param name="repository">Olay repository'si.</param>
    /// <param name="timeProvider">Saat kaynağı.</param>
    /// <param name="logger">Teknik log.</param>
    public SystemEventService(ISystemEventRepository repository, TimeProvider timeProvider, ILogger<SystemEventService> logger)
        : base(timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Olay seviyesinin teknik log karşılığı.</summary>
    /// <param name="level">Olay seviyesi.</param>
    /// <returns>Teknik log seviyesi.</returns>
    public static LogLevel ToLogLevel(EventLevel level) => level switch
    {
        EventLevel.Info => LogLevel.Information,
        EventLevel.Warning => LogLevel.Warning,
        EventLevel.Error => LogLevel.Error,
        EventLevel.Critical => LogLevel.Critical,
        _ => LogLevel.Information,
    };

    /// <inheritdoc />
    protected override void OnEventCreated(SystemEvent systemEvent)
    {
        ArgumentNullException.ThrowIfNull(systemEvent);
        LogEvent(_logger, ToLogLevel(systemEvent.Level), systemEvent.Type, systemEvent.Message);
    }

    /// <inheritdoc />
    protected override Task PersistAsync(SystemEvent systemEvent, CancellationToken cancellationToken) =>
        _repository.AddAsync(systemEvent, cancellationToken);

    /// <inheritdoc />
    protected override Task<SystemEvent?> LoadLatestAsync(CancellationToken cancellationToken) =>
        _repository.GetLatestAsync(cancellationToken);

    [LoggerMessage(EventId = 1001, Message = "Olay {EventType}: {EventMessage}")]
    private static partial void LogEvent(ILogger logger, LogLevel level, string eventType, string eventMessage);
}
