using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IPronunciationSuggestions"/> ortak davranışı: boş olmayan ve sözlükte (öneri ya da onaylı) bulunmayan yazılışları
/// <see cref="PronunciationStatus.Suggested"/> olarak ekler; aynı çağrıdaki tekrarlar da atlanır.
/// </summary>
public abstract class PronunciationSuggestionsBase : IPronunciationSuggestions
{
    private readonly IPronunciationRepository _repository;

    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Sözlük deposu.</param>
    protected PronunciationSuggestionsBase(IPronunciationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>Önerileri ekler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="source">Kaynak.</param>
    /// <param name="entries">Öneriler.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Eklenen sayısı.</returns>
    public async Task<int> AddSuggestionsAsync(Guid userId, Guid? conceptId, string? source, IReadOnlyList<PronunciationEntry> entries, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var added = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries.Where(e => !string.IsNullOrWhiteSpace(e.Written) && !string.IsNullOrWhiteSpace(e.Spoken)))
        {
            var key = Pronunciation.KeyOf(entry.Written);
            if (!seen.Add(key) || await _repository.FindByKeyAsync(userId, key, cancellationToken) is not null)
            {
                continue;
            }

            await _repository.AddAsync(Pronunciation.Create(userId, entry.Written, entry.Spoken, PronunciationStatus.Suggested, conceptId, source), cancellationToken);
            added++;
        }

        if (added > 0)
        {
            await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return added;
    }
}
