using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Mapping;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Base;

/// <summary>
/// <see cref="IConceptReader"/> ve <see cref="IConceptPronunciationSource"/> ortak davranışı: depodan kullanıcı filtresiyle takipsiz okur.
/// </summary>
public abstract class ConceptReaderBase : IConceptReader, IConceptPronunciationSource
{
    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Konsept deposu.</param>
    protected ConceptReaderBase(IConceptRepository repository)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>Konsept deposu.</summary>
    protected IConceptRepository Repository { get; }

    /// <summary>Konsepti getirir.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Ayrıntı ya da null.</returns>
    public virtual async Task<ConceptDetailDto?> GetAsync(Guid userId, Guid conceptId, CancellationToken ct = default) =>
        (await Repository.GetForUserAsync(conceptId, userId, ct))?.ToDetail();

    /// <summary>Konseptin okunuş önerileri.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Öneriler (konsept yoksa boş).</returns>
    public virtual async Task<IReadOnlyList<PronunciationEntry>> GetPronunciationsAsync(Guid userId, Guid conceptId, CancellationToken ct = default) =>
        (await GetAsync(userId, conceptId, ct))?.Document.Pronunciations ?? [];
}
