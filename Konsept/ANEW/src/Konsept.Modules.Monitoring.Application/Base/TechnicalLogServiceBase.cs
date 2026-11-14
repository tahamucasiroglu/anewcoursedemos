using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Monitoring.Application.Base;

/// <summary>
/// Teknik log servisinin ortak mantığı: Information kayıtlarını eler (Req 6), Contracts ↔ domain dönüşümü, saklama
/// sınırı (şimdi − 30 gün; 30 günlük korunur, daha eskisi silinir). Kalıcılık alt sınıftadır.
/// </summary>
/// <param name="timeProvider">Saat kaynağı.</param>
public abstract class TechnicalLogServiceBase(TimeProvider timeProvider) : ITechnicalLogService
{
    /// <summary>Veritabanında saklama süresi.</summary>
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    /// <summary>
    /// Gönderilen zamanın sunucu saatinden en fazla bu kadar sapmasına izin verilir; daha fazlası sunucu saatine çekilir
    /// (review L-1: istemci zamanı saklama süresini belirleyemez).
    /// </summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    /// <summary>Gets saat kaynağı.</summary>
    protected TimeProvider TimeProvider { get; } = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc />
    public async Task<int> WriteAsync(IReadOnlyList<TechnicalLogEntry> entries, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var now = TimeProvider.GetUtcNow();
        var logs = entries
            .Where(e => e is not null
                && e.Level >= LogEntryLevel.Warning
                && Enum.IsDefined(e.Level)
                && Enum.IsDefined(e.Source))
            .Select(e => TechnicalLog.Create(
                (e.OccurredAt - now).Duration() > MaxClockSkew ? now : e.OccurredAt,
                (TechnicalLogLevel)e.Level,
                (LogSource)e.Source,
                e.Message,
                e.Exception,
                e.CorrelationId,
                e.Category,
                e.AgentVersion))
            .ToList();
        if (logs.Count > 0)
        {
            await PersistAsync(logs, cancellationToken).ConfigureAwait(false);
        }

        return logs.Count;
    }

    /// <inheritdoc />
    public async Task<Result<int>> AcceptWebLogsAsync(IReadOnlyList<TechnicalLogEntry> entries, CancellationToken cancellationToken)
    {
        if (entries is null || entries.Count > MonitoringErrors.MaxLogsPerRequest)
        {
            return Result<int>.Failure(MonitoringErrors.TooManyLogs);
        }

        var fromWeb = entries.Select(e => e with { Source = LogEntrySource.Web, AgentVersion = null }).ToList();
        return Result<int>.Success(await WriteAsync(fromWeb, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public Task<int> PurgeExpiredAsync(CancellationToken cancellationToken) =>
        DeleteOlderThanAsync(TimeProvider.GetUtcNow() - Retention, cancellationToken);

    /// <summary>Kayıtları kalıcı yapar.</summary>
    /// <param name="logs">Kayıtlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task PersistAsync(IReadOnlyList<TechnicalLog> logs, CancellationToken cancellationToken);

    /// <summary>Sınırdan eski kayıtları siler.</summary>
    /// <param name="cutoff">Sınır.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Silinen kayıt sayısı.</returns>
    protected abstract Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken);
}
