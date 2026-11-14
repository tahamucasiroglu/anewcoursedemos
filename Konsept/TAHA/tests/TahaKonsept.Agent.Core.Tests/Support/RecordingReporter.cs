using System.Collections.Concurrent;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Tests.Support;

/// <summary>Raporları kaydeden sahte raporlayıcı; ilk sonuç raporu (tamamlandı/başarısız) <see cref="FirstOutcome"/> ile beklenebilir.</summary>
public sealed class RecordingReporter : IAgentReporter
{
    private readonly TaskCompletionSource<object> _outcome = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>İlerleme raporları.</summary>
    public ConcurrentQueue<StageProgressReport> Progress { get; } = new();

    /// <summary>Tamamlandı raporları.</summary>
    public ConcurrentQueue<StageResultReport> Completed { get; } = new();

    /// <summary>Başarısız raporları.</summary>
    public ConcurrentQueue<StageFailureReport> Failed { get; } = new();

    /// <summary>İlk sonuç raporu (<see cref="StageResultReport"/> ya da <see cref="StageFailureReport"/>).</summary>
    public Task<object> FirstOutcome => _outcome.Task;

    /// <inheritdoc />
    public Task ReportProgressAsync(StageProgressReport report, CancellationToken ct)
    {
        Progress.Enqueue(report);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportCompletedAsync(StageResultReport report, CancellationToken ct)
    {
        Completed.Enqueue(report);
        _outcome.TrySetResult(report);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportFailedAsync(StageFailureReport report, CancellationToken ct)
    {
        Failed.Enqueue(report);
        _outcome.TrySetResult(report);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task LogAsync(AgentLogEntry entry, CancellationToken ct) => Task.CompletedTask;

    /// <inheritdoc />
    public Task ReportSpeechPreviewAsync(SpeechPreviewResult result, CancellationToken ct) => Task.CompletedTask;
}
