using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.TextModels.Abstract;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Katalog kaydı + kimlik bilgisinden somut metin modeli üretir (platforma göre Anthropic / OpenAI / OpenRouter / GLM / Kimi).
/// </summary>
public interface ITextModelFactory
{
    /// <summary>Modeli oluşturur.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    Result<ITextModel> Create(ModelInfo model, ProviderCredential credential);
}
