using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Commands.Base;
using TahaKonsept.Agent.Core.Execution.Abstract;
using TahaKonsept.Agent.Core.Hub.Abstract;
using TahaKonsept.Agent.Core.SpeechPreview.Abstract;
using TahaKonsept.Agent.Core.Videos.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Core.Commands.Concrete;

/// <summary>Sunucu komut işleyicisi (hub <c>IAgentClient</c> uygulaması); arka plan hatalarını loglar.</summary>
public sealed class AgentCommandHandler : AgentCommandHandlerBase
{
    private readonly ILogger<AgentCommandHandler> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="jobs">İş çalıştırıcı.</param>
    /// <param name="videos">Video kütüphanesi.</param>
    /// <param name="previews">Ses önizleme.</param>
    /// <param name="hub">Hub bağlantısı.</param>
    /// <param name="logger">Logger.</param>
    public AgentCommandHandler(IJobRunner jobs, IVideoLibrary videos, ISpeechPreviewService previews, IAgentHubConnection hub, ILogger<AgentCommandHandler> logger)
        : base(jobs, videos, previews, hub)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override void OnVideoCommand(string command, Guid videoId, Result result)
    {
        if (result.IsSuccess)
        {
            _logger.LogInformation("Sunucu komutu işlendi: {Command} (video {VideoId}).", command, videoId);
        }
        else
        {
            _logger.LogWarning("Sunucu komutu başarısız: {Command} (video {VideoId}): {Error}", command, videoId, result.Error.Message);
        }
    }

    /// <inheritdoc />
    protected override void OnBackgroundFailed(Exception exception) =>
        _logger.LogWarning(exception, "Sunucu komutu arka planda başarısız oldu.");
}
