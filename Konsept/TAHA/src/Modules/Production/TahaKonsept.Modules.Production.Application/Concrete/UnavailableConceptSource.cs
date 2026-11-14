using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Concepts adaptörü kaydedilmemişse kullanılan yedek <c>IConceptSource</c> (TryAdd): her zaman <c>null</c> döner — sıraya ekleme
/// "konsept bulunamadı" ile reddedilir. Entegrasyonda Concepts <c>IConceptReader</c>'ı saran adaptör bunun yerine kaydedilmelidir.
/// </summary>
public sealed class UnavailableConceptSource : ConceptSourceBase
{
    /// <summary>Her zaman <c>null</c>.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="conceptId">Konsept.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns><c>null</c>.</returns>
    protected override Task<ConceptSnapshot?> ReadAsync(Guid userId, Guid conceptId, CancellationToken cancellationToken) =>
        Task.FromResult<ConceptSnapshot?>(null);
}
