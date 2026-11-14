using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Execution.Base;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Execution.Concrete;

/// <summary>Loglayan iş çalıştırıcı; eş zamanlılık <see cref="AgentOptions.MaxConcurrentStages"/>.</summary>
public sealed class JobRunner : JobRunnerBase
{
    private readonly ILogger<JobRunner> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="dispatcher">Aşama dağıtıcısı.</param>
    /// <param name="reporter">Raporlayıcı.</param>
    /// <param name="api">API istemcisi.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="time">Zaman sağlayıcı.</param>
    /// <param name="options">Seçenekler.</param>
    /// <param name="logger">Logger.</param>
    public JobRunner(
        IStageExecutorDispatcher dispatcher,
        IAgentReporter reporter,
        IAgentApiClient api,
        IAgentPaths paths,
        TimeProvider time,
        AgentOptions options,
        ILogger<JobRunner> logger)
        : base(dispatcher, reporter, api, paths, time, options.MaxConcurrentStages)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override void OnSubmitted(StageCommand command) =>
        _logger.LogInformation("Aşama kuyruğa alındı: {Stage} iş {JobId} sahne {Scene}", command.Stage, command.JobId, command.SceneIndex);

    /// <inheritdoc />
    protected override void OnStageCrashed(StageCommand command, Exception exception) =>
        _logger.LogError(exception, "Aşama çöktü: {Stage} iş {JobId}", command.Stage, command.JobId);

    /// <inheritdoc />
    protected override void OnStageFinished(StageCommand command, string outcome, TimeSpan elapsed) =>
        _logger.LogInformation("Aşama bitti: {Stage} iş {JobId} → {Outcome} ({Elapsed} ms)", command.Stage, command.JobId, outcome, (long)elapsed.TotalMilliseconds);

    /// <inheritdoc />
    protected override void OnStopTimedOut() => _logger.LogWarning("İş çalıştırıcı kapanışta zamanında durmadı.");
}
