using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.AiModels.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.SpeechModels.Abstract;

/// <summary>
/// Online ses modeli (Gemini TTS, ElevenLabs, Azure Neural, Cartesia, OpenAI TTS). Somut sınıflar ayrı bir adımda
/// <c>Infrastructure/SpeechModels/Concrete</c> altına eklenecek; ortak davranış <c>SpeechModelBase</c>'de.
/// </summary>
public interface ISpeechModel : IAiModel
{
    /// <summary>Metni seslendirir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<SpeechResult>> SynthesizeAsync(SpeechRequest request, CancellationToken ct);
}
