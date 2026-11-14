using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.SpeechPreview.Abstract;
using TahaKonsept.Agent.Core.Videos.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Agent.Core.Commands.Base;

/// <summary>
/// Sunucu → ajan komut işleyici tabanı (<see cref="IAgentClient"/>). Hub alma döngüsünü bloklamamak için uzun işler
/// (aşama, ses önizleme, yetenek yenileme) arka planda yürür. Video işlemlerinin hataları hub'a günlük olarak gider.
/// </summary>
public abstract class AgentCommandHandlerBase : IAgentClient
{
    private readonly IJobRunner _jobs;
    private readonly IVideoLibrary _videos;
    private readonly ISpeechPreviewService _previews;
    private readonly IAgentHubConnection _hub;

    /// <summary>Oluşturur.</summary>
    /// <param name="jobs">İş çalıştırıcı.</param>
    /// <param name="videos">Video kütüphanesi.</param>
    /// <param name="previews">Ses önizleme.</param>
    /// <param name="hub">Hub bağlantısı.</param>
    protected AgentCommandHandlerBase(IJobRunner jobs, IVideoLibrary videos, ISpeechPreviewService previews, IAgentHubConnection hub)
    {
        _jobs = jobs;
        _videos = videos;
        _previews = previews;
        _hub = hub;
    }

    /// <inheritdoc />
    public Task ExecuteStage(StageCommand command)
    {
        _jobs.Submit(command);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CancelJob(Guid jobId)
    {
        _jobs.CancelJob(jobId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PauseJob(Guid jobId)
    {
        _jobs.PauseJob(jobId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OpenFolder(Guid videoId) => ReportIfFailedAsync(_videos.OpenFolder(videoId), "Klasör açılamadı");

    /// <inheritdoc />
    public Task OpenInSystemPlayer(Guid videoId) => ReportIfFailedAsync(_videos.OpenInSystemPlayer(videoId), "Video açılamadı");

    /// <inheritdoc />
    public Task DeleteVideo(Guid videoId) => ReportIfFailedAsync(_videos.Delete(videoId), "Video silinemedi");

    /// <inheritdoc />
    public Task PreviewSpeech(SpeechPreviewCommand command)
    {
        RunInBackground(async () =>
        {
            var result = await _previews.CreatePreviewAsync(command, CancellationToken.None);
            await _hub.ReportSpeechPreviewAsync(result, CancellationToken.None);
        });
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RefreshCapabilities()
    {
        RunInBackground(() => _hub.SendHelloAsync(CancellationToken.None));
        return Task.CompletedTask;
    }

    /// <summary>Arka plan işi hata verdiğinde çağrılır.</summary>
    /// <param name="exception">İstisna.</param>
    protected virtual void OnBackgroundFailed(Exception exception)
    {
    }

    private void RunInBackground(Func<Task> work) =>
        _ = Task.Run(async () =>
        {
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                OnBackgroundFailed(ex);
            }
        });

    private Task ReportIfFailedAsync(Result result, string prefix) =>
        result.IsSuccess
            ? Task.CompletedTask
            : _hub.LogAsync(
                new AgentLogEntry
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = LogLevelKind.Warning,
                    Category = "Videos",
                    Message = prefix + ": " + result.Error.Message,
                },
                CancellationToken.None);
}
