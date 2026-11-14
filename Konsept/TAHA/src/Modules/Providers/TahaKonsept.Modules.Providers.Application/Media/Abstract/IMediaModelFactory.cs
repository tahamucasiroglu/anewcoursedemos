using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.ImageModels.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.SpeechModels.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Media.Abstract;

/// <summary>
/// Katalog kaydı + kimlik bilgisinden somut online görsel / ses modeli üretir (platforma göre Gemini, OpenAI, fal, BFL, Z.ai,
/// Replicate, ElevenLabs, Azure, Cartesia). Önemli üyeler: <see cref="CreateImage"/>, <see cref="CreateSpeech"/>.
/// </summary>
public interface IMediaModelFactory
{
    /// <summary>Görsel modelini oluşturur.</summary>
    /// <param name="model">Katalog kaydı (online görsel).</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <returns>Model ya da <c>providers.model_not_supported</c> / <c>providers.invalid_request</c>.</returns>
    Result<IImageModel> CreateImage(ModelInfo model, ProviderCredential credential);

    /// <summary>Ses modelini oluşturur.</summary>
    /// <param name="model">Katalog kaydı (online ses).</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <returns>Model ya da hata.</returns>
    Result<ISpeechModel> CreateSpeech(ModelInfo model, ProviderCredential credential);
}
