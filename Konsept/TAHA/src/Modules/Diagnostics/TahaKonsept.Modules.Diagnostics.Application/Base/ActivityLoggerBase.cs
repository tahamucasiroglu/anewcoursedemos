using Microsoft.Extensions.Logging;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Base;

/// <summary>
/// <see cref="IActivityLogger"/> ortak davranışı: kısa yollar (Information/Warning/Error/Critical/LogTimed), ayrıntının JSON'a
/// çevrilmesi, kaydın Serilog'a da yansıtılması ve yazım hatasının yutulması (günlük yazmak işi asla bozmaz).
/// Türetilen sınıf yalnızca <see cref="WriteAsync"/> (kalıcı yazım) ve <see cref="ResolveCurrentUserId"/> yazar.
/// </summary>
public abstract class ActivityLoggerBase : IActivityLogger
{
    /// <summary>Oluşturur.</summary>
    /// <param name="logger">Serilog/MEL günlükçüsü.</param>
    protected ActivityLoggerBase(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Teknik günlükçü.</summary>
    protected ILogger Logger { get; }

    /// <summary>Kayıt yazar (istisna fırlatmaz; iptal hariç).</summary>
    /// <param name="level">Seviye.</param>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="durationMs">Süre (ms).</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public async Task LogAsync(
        LogLevelKind level,
        string source,
        string message,
        object? details = null,
        long? durationMs = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        Mirror(level, source, message);
        try
        {
            var owner = userId ?? ResolveCurrentUserId() ?? Guid.Empty;
            var entry = ActivityLog.Create(owner, level, source, message, ActivityDetailsJson.Serialize(details), durationMs);
            await WriteAsync(entry, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) // Günlük yazımı çağıranın işini asla bozmamalı.
        {
            Logger.LogWarning(ex, "İşlem günlüğü veritabanına yazılamadı: {Source} {Message}", source, message);
        }
    }

    /// <summary>Bilgi kaydı.</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="durationMs">Süre (ms).</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task InformationAsync(string source, string message, object? details = null, long? durationMs = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevelKind.Information, source, message, details, durationMs, userId, cancellationToken);

    /// <summary>Uyarı kaydı.</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task WarningAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevelKind.Warning, source, message, details, null, userId, cancellationToken);

    /// <summary>Hata kaydı.</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task ErrorAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevelKind.Error, source, message, details, null, userId, cancellationToken);

    /// <summary>Kritik kayıt.</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task CriticalAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevelKind.Critical, source, message, details, null, userId, cancellationToken);

    /// <summary>Süreli işlem kaydı ("— süre 57 sn, başlangıç 10:30, bitiş 11:00").</summary>
    /// <param name="source">Kaynak.</param>
    /// <param name="message">Mesaj.</param>
    /// <param name="startedAtUtc">Başlangıç.</param>
    /// <param name="finishedAtUtc">Bitiş.</param>
    /// <param name="level">Seviye.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <param name="userId">Sahip.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public Task LogTimedAsync(
        string source,
        string message,
        DateTimeOffset startedAtUtc,
        DateTimeOffset finishedAtUtc,
        LogLevelKind level = LogLevelKind.Information,
        object? details = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var durationMs = (long)Math.Max(0, (finishedAtUtc - startedAtUtc).TotalMilliseconds);
        return LogAsync(level, source, ActivityDurationText.Describe(message, startedAtUtc, finishedAtUtc), details, durationMs, userId, cancellationToken);
    }

    /// <summary>Hazır kaydı kalıcı olarak yazar.</summary>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task WriteAsync(ActivityLog entry, CancellationToken cancellationToken);

    /// <summary>İstekteki oturum kullanıcısı (yoksa <c>null</c>).</summary>
    /// <returns>Kullanıcı kimliği.</returns>
    protected abstract Guid? ResolveCurrentUserId();

    private void Mirror(LogLevelKind level, string source, string message)
    {
        var msLevel = level switch
        {
            LogLevelKind.Debug => LogLevel.Debug,
            LogLevelKind.Warning => LogLevel.Warning,
            LogLevelKind.Error => LogLevel.Error,
            LogLevelKind.Critical => LogLevel.Critical,
            _ => LogLevel.Information,
        };
        if (Logger.IsEnabled(msLevel))
        {
            Logger.Log(msLevel, "[Günlük] {Source}: {Message}", source, message);
        }
    }
}
