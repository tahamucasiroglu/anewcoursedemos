using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// PUBLIC sözleşme (Production Normalize aşaması): konsept üretilirken LLM'in önerdiği okunuşları (yazılış → okunuş) verir.
/// Sözlükte olmayan yabancı özel isimler için "öneri" kaynağıdır.
/// </summary>
public interface IConceptPronunciationSource
{
    /// <summary>Konseptin okunuş önerileri.</summary>
    /// <param name="userId">Sahip kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Öneriler (konsept yoksa boş).</returns>
    Task<IReadOnlyList<PronunciationEntry>> GetPronunciationsAsync(Guid userId, Guid conceptId, CancellationToken ct = default);
}
