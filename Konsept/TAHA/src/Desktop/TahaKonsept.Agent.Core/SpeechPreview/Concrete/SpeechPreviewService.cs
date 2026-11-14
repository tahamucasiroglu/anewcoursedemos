using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Abstractions.Paths.Abstract;
using TahaKonsept.Agent.Abstractions.Speech.Abstract;
using TahaKonsept.Agent.Core.SpeechPreview.Base;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.SpeechPreview.Concrete;

/// <summary>Loglayan ses önizleme servisi.</summary>
public sealed class SpeechPreviewService : SpeechPreviewServiceBase
{
    private readonly ILogger<SpeechPreviewService> _logger;

    /// <summary>Oluşturur.</summary>
    /// <param name="engines">Ses motorları.</param>
    /// <param name="paths">Ajan klasörleri.</param>
    /// <param name="logger">Logger.</param>
    public SpeechPreviewService(IEnumerable<ISpeechEngine> engines, IAgentPaths paths, ILogger<SpeechPreviewService> logger)
        : base(engines, paths)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override void OnPreviewFailed(SpeechPreviewCommand command, Exception exception) =>
        _logger.LogWarning(exception, "Ses önizlemesi başarısız ({Engine}).", command.EngineId);
}
