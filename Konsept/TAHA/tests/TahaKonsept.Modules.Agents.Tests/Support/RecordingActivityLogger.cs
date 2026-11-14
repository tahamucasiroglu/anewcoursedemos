using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Yazılan işlem günlüğü kayıtlarını bellekte toplayan sahte <see cref="IActivityLogger"/>.
/// </summary>
public sealed class RecordingActivityLogger : IActivityLogger
{
    /// <summary>Yakalanan kayıtlar.</summary>
    public List<RecordedActivity> Entries { get; } = [];

    /// <inheritdoc />
    public Task LogAsync(LogLevelKind level, string source, string message, object? details = null, long? durationMs = null, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        Entries.Add(new RecordedActivity(level, source, message, details, userId));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InformationAsync(string source, string message, object? details = null, long? durationMs = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevelKind.Information, source, message, details, durationMs, userId, cancellationToken);

    /// <inheritdoc />
    public Task WarningAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevelKind.Warning, source, message, details, null, userId, cancellationToken);

    /// <inheritdoc />
    public Task ErrorAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevelKind.Error, source, message, details, null, userId, cancellationToken);

    /// <inheritdoc />
    public Task CriticalAsync(string source, string message, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(LogLevelKind.Critical, source, message, details, null, userId, cancellationToken);

    /// <inheritdoc />
    public Task LogTimedAsync(string source, string message, DateTimeOffset startedAtUtc, DateTimeOffset finishedAtUtc, LogLevelKind level = LogLevelKind.Information, object? details = null, Guid? userId = null, CancellationToken cancellationToken = default) =>
        LogAsync(level, source, message, details, (long)(finishedAtUtc - startedAtUtc).TotalMilliseconds, userId, cancellationToken);
}
