using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Konseptlerden gelen okunuş önerilerini sözlüğe "öneri" olarak ekler (aynı yazılış varsa dokunmaz). Concepts modülü de çağırabilir
/// (Production.Application referansıyla). Kaydeder.
/// </summary>
public interface IPronunciationSuggestions
{
    /// <summary>Önerileri ekler; eklenen sayısını döner.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="source">Kaynak açıklaması.</param>
    /// <param name="entries">Öneriler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<int> AddSuggestionsAsync(Guid userId, Guid? conceptId, string? source, IReadOnlyList<PronunciationEntry> entries, CancellationToken cancellationToken);
}
