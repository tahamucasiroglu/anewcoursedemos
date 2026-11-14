using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using TahaKonsept.Agent.Core.Configuration;
using TahaKonsept.Agent.Core.Execution;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Execution.Concrete;
using TahaKonsept.Agent.Core.Http.Abstract;
using TahaKonsept.Agent.Core.Tests.Support;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Production;
using Xunit;

namespace TahaKonsept.Agent.Core.Tests.Execution;

/// <summary>İş çalıştırıcı: başarı raporu, hata raporu, duraklatma (dosyalar kalır) ve iptal (çalışma alanı silinir).</summary>
public sealed class JobRunnerTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly TestAgentPaths _paths;
    private readonly IStageExecutorDispatcher _dispatcher = Substitute.For<IStageExecutorDispatcher>();
    private readonly IAgentApiClient _api = Substitute.For<IAgentApiClient>();
    private readonly RecordingReporter _reporter = new();

    /// <summary>Kurar.</summary>
    public JobRunnerTests()
    {
        _paths = new TestAgentPaths(_temp.Path);
    }

    /// <inheritdoc />
    public void Dispose() => _temp.Dispose();

    /// <summary>Başarılı aşama tamamlandı raporu (artefakt + çıktı JSON) gönderir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Submit_Success_ReportsCompleted()
    {
        _dispatcher.DispatchAsync(Arg.Any<StageExecutionContext>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            call.Arg<StageExecutionContext>().Report(50, "yarı");
            return Task.FromResult(Result.Success(new StageExecutionOutput([new ProducedArtifact { Name = "a.png" }], "{\"ok\":true}")));
        });
        var runner = CreateRunner();
        var command = Command(PipelineStage.Images);

        runner.Submit(command);
        var outcome = await _reporter.FirstOutcome.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        var completed = outcome.ShouldBeOfType<StageResultReport>();
        completed.CommandId.ShouldBe(command.CommandId);
        completed.Artifacts.Single().Name.ShouldBe("a.png");
        completed.OutputJson.ShouldBe("{\"ok\":true}");
        completed.CostUsd.ShouldBe(0m);
        _reporter.Progress.ShouldNotBeEmpty();
        await runner.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Yürütücü hatası başarısız raporuna (kod, mesaj, yeniden denenebilirlik) çevrilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Submit_Failure_ReportsFailed()
    {
        _dispatcher.DispatchAsync(Arg.Any<StageExecutionContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<StageExecutionOutput>(Error.Validation("agent.scene_invalid", "lint"))));
        var runner = CreateRunner();

        runner.Submit(Command(PipelineStage.SceneRender));
        var outcome = await _reporter.FirstOutcome.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        var failed = outcome.ShouldBeOfType<StageFailureReport>();
        failed.ErrorCode.ShouldBe("agent.scene_invalid");
        failed.Retryable.ShouldBeFalse();
        failed.Cancelled.ShouldBeFalse();
        await runner.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>Duraklatılan iş <c>agent.paused</c> raporlar ve çalışma alanı dosyaları korunur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Pause_CancelsStep_KeepsFiles()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher.DispatchAsync(Arg.Any<StageExecutionContext>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var context = call.Arg<StageExecutionContext>();
            await File.WriteAllTextAsync(Path.Combine(context.JobDir, "checkpoint.txt"), "1", CancellationToken.None);
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
            return Result.Success(new StageExecutionOutput([]));
        });
        var runner = CreateRunner();
        var command = Command(PipelineStage.SceneRender);

        runner.Submit(command);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        runner.PauseJob(command.JobId);
        var outcome = await _reporter.FirstOutcome.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        var failed = outcome.ShouldBeOfType<StageFailureReport>();
        failed.ErrorCode.ShouldBe("agent.paused");
        failed.Cancelled.ShouldBeTrue();
        failed.Retryable.ShouldBeTrue();
        File.Exists(Path.Combine(_paths.WorkspaceDir, command.JobId.ToString("N"), "checkpoint.txt")).ShouldBeTrue();
        await runner.StopAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>İptal edilen iş <c>agent.cancelled</c> raporlar ve çalışma alanı silinir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Cancel_CancelsStep_DeletesWorkspace()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher.DispatchAsync(Arg.Any<StageExecutionContext>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            started.TrySetResult();
            await Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
            return Result.Success(new StageExecutionOutput([]));
        });
        var runner = CreateRunner();
        var command = Command(PipelineStage.Speech);

        runner.Submit(command);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        runner.CancelJob(command.JobId);
        var outcome = await _reporter.FirstOutcome.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await runner.StopAsync(TestContext.Current.CancellationToken);

        outcome.ShouldBeOfType<StageFailureReport>().ErrorCode.ShouldBe("agent.cancelled");
        Directory.Exists(Path.Combine(_paths.WorkspaceDir, command.JobId.ToString("N"))).ShouldBeFalse();
    }

    /// <summary>
    /// Paylaşılan çalışma alanında aynı adlı girdi farklı artefaktsa yeniden indirilir (eski sürümün görseli kullanılmaz);
    /// aynı artefakt ise indirme atlanır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Inputs_are_cached_by_artifact_id_not_name()
    {
        var contents = new Queue<string>(["eski", "yeni"]);
        _api.DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            await File.WriteAllTextAsync(call.ArgAt<string>(1), contents.Dequeue(), CancellationToken.None);
            return Result.Success(3L);
        });
        var seen = new List<string>();
        _dispatcher.DispatchAsync(Arg.Any<StageExecutionContext>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            seen.Add(await File.ReadAllTextAsync(call.Arg<StageExecutionContext>().FindInput("scene-1.png")!, CancellationToken.None));
            return Result.Success(new StageExecutionOutput([]));
        });
        var runner = CreateRunner();
        var workspace = Guid.NewGuid();
        var first = Guid.NewGuid();

        var submitted = 0;
        foreach (var artifactId in new[] { first, first, Guid.NewGuid() })
        {
            runner.Submit(Command(PipelineStage.SceneRender) with
            {
                JobId = workspace,
                Artifacts = [new ArtifactDownload { ArtifactId = artifactId, Name = "scene-1.png", Url = "http://pi/a/" + artifactId }],
            });
            submitted++;
            for (var wait = 0; _reporter.Completed.Count + _reporter.Failed.Count < submitted && wait < 200; wait++)
            {
                await Task.Delay(50, TestContext.Current.CancellationToken);
            }
        }

        _reporter.Failed.ShouldBeEmpty();

        await runner.StopAsync(TestContext.Current.CancellationToken);
        seen.ShouldBe(["eski", "eski", "yeni"]);
        await _api.Received(2).DownloadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IProgress<double>?>(), Arg.Any<CancellationToken>());
    }

    private static StageCommand Command(PipelineStage stage) => new()
    {
        CommandId = Guid.NewGuid(),
        JobId = Guid.NewGuid(),
        VideoId = Guid.NewGuid(),
        Stage = stage,
        PayloadJson = "{}",
    };

    private JobRunner CreateRunner() =>
        new(_dispatcher, _reporter, _api, _paths, new WorkspaceUsage(), TimeProvider.System, new AgentOptions(), NullLogger<JobRunner>.Instance);
}
