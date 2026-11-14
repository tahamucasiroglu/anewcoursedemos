using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Abstract;

/// <summary>
/// Model kataloğu (gömülü <c>catalog.json</c>): tüm LLM, online/yerel ses, online/yerel görsel ve video motorları.
/// Önemli üyeler: <see cref="All"/>, <see cref="Find"/> (OpenRouter için <c>openrouter:&lt;id&gt;</c> desteği), <see cref="Query"/>.
/// </summary>
public interface IModelCatalog
{
    /// <summary>Tüm kayıtlar (katalog sırasıyla).</summary>
    IReadOnlyList<ModelInfo> All { get; }

    /// <summary>Anahtara göre bulur. <c>openrouter:&lt;sağlayıcı/model&gt;</c> verilirse OpenRouter kaydı bu kimlikle döner.</summary>
    /// <param name="modelKey">Katalog anahtarı.</param>
    /// <returns>Kayıt ya da null.</returns>
    ModelInfo? Find(string modelKey);

    /// <summary>Yetenek ve çalışma yerine göre süzer.</summary>
    /// <param name="capability">Yetenek (null = hepsi).</param>
    /// <param name="runsOn">Çalışma yeri (null = hepsi).</param>
    /// <returns>Süzülmüş liste.</returns>
    Result<IReadOnlyList<ModelInfo>> Query(ModelCapability? capability, ModelRunsOn? runsOn);
}
