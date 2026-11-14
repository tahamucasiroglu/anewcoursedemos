using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.AiModels.Abstract;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Application.TextModels.Abstract;

/// <summary>
/// Metin üreten online model (LLM). Somut sınıflar: <c>AnthropicTextModel</c>, <c>OpenAiTextModel</c>, <c>OpenRouterTextModel</c>,
/// <c>GlmTextModel</c>, <c>KimiTextModel</c>. Ortak davranış <c>TextModelBase</c>'de.
/// </summary>
public interface ITextModel : IAiModel
{
    /// <summary>Yapılandırılmış JSON çıktı → <typeparamref name="T"/> (bir onarım denemesiyle).</summary>
    /// <typeparam name="T">Çıktı türü (nesne).</typeparam>
    /// <param name="request">İstek (şema boşsa <typeparamref name="T"/>'den üretilir).</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<TextResult<T>>> RunAsync<T>(TextRequest request, CancellationToken ct);

    /// <summary>Serbest metin çıktı.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    Task<Result<TextResult<string>>> RunTextAsync(TextRequest request, CancellationToken ct);
}
