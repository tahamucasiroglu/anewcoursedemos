using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IConceptSource"/> taban sınıfı: entegrasyon adaptörü (Concepts <c>IConceptReader</c> → <see cref="ConceptSnapshot"/>) yalnızca
/// <see cref="ReadAsync"/>'i uygular; boş kimlik baştan elenir.
/// </summary>
public abstract class ConceptSourceBase : IConceptSource
{
    /// <summary>Kullanıcının konseptini okur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konsept ya da <c>null</c>.</returns>
    public Task<ConceptSnapshot?> GetAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        conceptId == Guid.Empty ? Task.FromResult<ConceptSnapshot?>(null) : ReadAsync(userId, conceptId, cancellationToken);

    /// <summary>Konsepti asıl kaynaktan okur.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Konsept ya da <c>null</c>.</returns>
    protected abstract Task<ConceptSnapshot?> ReadAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken);
}
