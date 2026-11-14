using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konu üreticisi (LLM, ucuz model): kanal + hafıza listesi + yön/fikir ile yalnız başlık + kanca ister, birbirinin ve hafızanın
/// tekrarlarını eler. Hem konu önerisi hem "fikirden N farklı açı" için kullanılır.
/// </summary>
public interface ITopicGenerator
{
    /// <summary>Başlık üretir.</summary>
    /// <param name="context">Bağlam.</param>
    /// <param name="count">İstenen adet.</param>
    /// <param name="hint">Yön (isteğe bağlı).</param>
    /// <param name="idea">Fikir (isteğe bağlı).</param>
    /// <param name="memory">Hafıza.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Öneriler.</returns>
    Task<Result<TopicBatch>> GenerateAsync(ConceptGenerationContext context, int count, string? hint, string? idea, TopicMemorySnapshot memory, CancellationToken cancellationToken = default);
}
