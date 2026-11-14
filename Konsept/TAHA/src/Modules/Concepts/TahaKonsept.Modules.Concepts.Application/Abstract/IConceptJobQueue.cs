using TahaKonsept.Modules.Concepts.Application.Models;

namespace TahaKonsept.Modules.Concepts.Application.Abstract;

/// <summary>
/// Konsept üretim işi kuyruğu (singleton, <c>Channel</c>): API ekler, arka plan işçisi tek tek okur (sırayla).
/// </summary>
public interface IConceptJobQueue
{
    /// <summary>İşi kuyruğa ekler.</summary>
    /// <param name="job">İş.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    ValueTask EnqueueAsync(ConceptJob job, CancellationToken cancellationToken = default);

    /// <summary>Kuyruktaki işleri sırayla okur.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>İş akışı.</returns>
    IAsyncEnumerable<ConceptJob> ReadAllAsync(CancellationToken cancellationToken);
}
