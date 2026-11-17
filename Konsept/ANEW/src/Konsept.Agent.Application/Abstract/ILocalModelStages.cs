using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Yerel model aşamaları (spec 0028 Req 7–11): bileşen listesindeki çalıştırma şablonuyla görsel ve seslendirme üretir. Model kurulu değilse
/// <see cref="AgentStageReply.ToolMissing"/>, ekran kartı hatasında <see cref="AgentStageReply.GpuUnavailable"/> döner.
/// </summary>
public interface ILocalModelStages
{
    /// <summary>Sahne görselleri (<c>image.png</c>).</summary>
    /// <param name="command">Komut (yerel görsel modeli).</param>
    /// <param name="sceneDirectory">Sahne sırası → klasör.</param>
    /// <param name="cancellationToken">Durdur.</param>
    /// <returns>Cevap.</returns>
    Task<AgentStageReply> ImagesAsync(AgentStageCommand command, Func<int, string> sceneDirectory, CancellationToken cancellationToken);

    /// <summary>Seslendirme (<c>voice.wav</c> + konuşma süresi), sahne süresine oturtulmuş.</summary>
    /// <param name="command">Komut (yerel ses modeli).</param>
    /// <param name="workDirectory">İş klasörü.</param>
    /// <param name="sceneDirectory">Sahne sırası → klasör.</param>
    /// <param name="ffmpeg">FFmpeg yolu (oturtma).</param>
    /// <param name="cancellationToken">Durdur.</param>
    /// <returns>Cevap.</returns>
    Task<AgentStageReply> VoiceAsync(AgentStageCommand command, string workDirectory, Func<int, string> sceneDirectory, string ffmpeg, CancellationToken cancellationToken);
}
