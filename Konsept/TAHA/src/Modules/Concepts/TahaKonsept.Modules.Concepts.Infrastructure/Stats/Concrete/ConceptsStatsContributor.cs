using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Diagnostics.Application.Base;
using TahaKonsept.Modules.Diagnostics.Application.Common;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Stats.Concrete;

/// <summary>
/// Yönetim paneli için Concepts katkıcısı (Diagnostics <c>IStatsContributor</c>): <see cref="StatsKeys.ConceptsTotal"/> (silinenler dahil
/// tüm konseptler) ve durum başına ek değerler (<c>concepts.generated</c>, <c>concepts.approved</c>, <c>concepts.deleted</c>).
/// </summary>
public sealed class ConceptsStatsContributor : StatsContributorBase
{
    private readonly IConceptRepository _repository;

    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Konsept deposu.</param>
    public ConceptsStatsContributor(IConceptRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>Değerleri yazar.</summary>
    /// <param name="stats">Sözlük.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected override async Task ContributeAsync(IDictionary<string, object> stats, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stats);
        var counts = await _repository.CountByStatusAsync(cancellationToken);
        stats[StatsKeys.ConceptsTotal] = counts.Values.Sum();
        stats["concepts.generated"] = counts.GetValueOrDefault(ConceptStatus.Generated);
        stats["concepts.approved"] = counts.GetValueOrDefault(ConceptStatus.Approved);
        stats["concepts.deleted"] = counts.GetValueOrDefault(ConceptStatus.Deleted);
    }
}
