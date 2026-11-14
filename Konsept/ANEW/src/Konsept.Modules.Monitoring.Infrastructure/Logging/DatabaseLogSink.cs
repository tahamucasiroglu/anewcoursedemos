using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Konsept.Modules.Monitoring.Infrastructure.Logging;

/// <summary>
/// Api'nin Warning+ loglarını toplu olarak veritabanındaki teknik loga yazan Serilog sink'i (spec 0008 Req 6).
/// Çerçeve adaptörü: yazma <see cref="ITechnicalLogWriter"/>'dadır. Döngü olmasın diye EF Core, Npgsql ve bu
/// sink'in kendi kategorisi yazılmaz (plan 0008, Risk 4). Veritabanı kapalıyken yazma hatası en fazla 5 dakikada
/// bir dosya loguna Warning olarak düşer; uygulama çalışmaya devam eder.
/// </summary>
/// <param name="services">Kök servis sağlayıcı.</param>
/// <param name="source">Kaynak uygulama.</param>
/// <param name="reportFailure">Yazma hatasını bildiren eylem (dosya loguna).</param>
/// <param name="timeProvider">Saat kaynağı (hata bildirimi aralığı için).</param>
public sealed class DatabaseLogSink(IServiceProvider services, LogEntrySource source, Action<Exception> reportFailure, TimeProvider timeProvider)
    : IBatchedLogEventSink
{
    /// <summary>Veritabanına yazılmayan kategori önekleri.</summary>
    public static readonly string[] ExcludedCategories =
        ["Microsoft.EntityFrameworkCore", "Npgsql", "Konsept.Modules.Monitoring.Infrastructure.Logging"];

    private static readonly TimeSpan FailureReportInterval = TimeSpan.FromMinutes(5);
    private DateTimeOffset _lastFailureReport = DateTimeOffset.MinValue;

    /// <summary>Olay veritabanına yazılacak mı (seviye Warning+, kategori dışlanmamış).</summary>
    /// <param name="logEvent">Olay.</param>
    /// <returns>Yazılacaksa <c>true</c>.</returns>
    public static bool ShouldPersist(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        if (logEvent.Level < LogEventLevel.Warning)
        {
            return false;
        }

        var category = Text(logEvent, "SourceContext");
        return category is null || !ExcludedCategories.Any(c => category.StartsWith(c, StringComparison.Ordinal));
    }

    /// <summary>Serilog olayını teknik log kaydına çevirir.</summary>
    /// <param name="logEvent">Olay.</param>
    /// <param name="source">Kaynak uygulama.</param>
    /// <returns>Kayıt.</returns>
    public static TechnicalLogEntry ToEntry(LogEvent logEvent, LogEntrySource source)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        return new TechnicalLogEntry(
            logEvent.Timestamp,
            ToLevel(logEvent.Level),
            source,
            KonseptLogging.RenderMessage(logEvent),
            logEvent.Exception?.ToString(),
            Text(logEvent, KonseptLogging.CorrelationProperty),
            Text(logEvent, "SourceContext"));
    }

    /// <inheritdoc />
    public async Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        ArgumentNullException.ThrowIfNull(batch);
        var entries = batch.Where(ShouldPersist).Select(e => ToEntry(e, source)).ToList();
        if (entries.Count == 0)
        {
            return;
        }

        try
        {
            await using var scope = services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ITechnicalLogWriter>().WriteAsync(entries, CancellationToken.None).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Log yazımı uygulamayı asla düşürmemeli (DB kapalı olabilir).
        catch (Exception ex)
#pragma warning restore CA1031
        {
            var now = timeProvider.GetUtcNow();
            if (now - _lastFailureReport >= FailureReportInterval)
            {
                _lastFailureReport = now;
                reportFailure(ex);
            }
        }
    }

    private static LogEntryLevel ToLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Warning => LogEntryLevel.Warning,
        LogEventLevel.Error => LogEntryLevel.Error,
        LogEventLevel.Fatal => LogEntryLevel.Critical,
        _ => LogEntryLevel.Information,
    };

    private static string? Text(LogEvent logEvent, string property) =>
        logEvent.Properties.TryGetValue(property, out var value)
            ? value is ScalarValue { Value: string s } ? s : value.ToString()
            : null;
}
