using TahaKonsept.Modules.Diagnostics.Application.Abstract;

namespace TahaKonsept.Modules.Diagnostics.Application.Base;

/// <summary>
/// <see cref="IStatsContributor"/> için kolaylık tabanı: türetilen sınıf <see cref="ContributeAsync"/> içinde sözlüğe yazar
/// (<c>stats[StatsKeys.VideosTotal] = count;</c>), taban salt okunur sözlüğe çevirir. Diğer modüller de bundan türeyebilir.
/// </summary>
public abstract class StatsContributorBase : IStatsContributor
{
    /// <summary>İstatistikleri üretir.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Anahtar → değer.</returns>
    public async Task<IReadOnlyDictionary<string, object>> GetStatsAsync(CancellationToken cancellationToken)
    {
        var stats = new Dictionary<string, object>(StringComparer.Ordinal);
        await ContributeAsync(stats, cancellationToken);
        return stats;
    }

    /// <summary>Sözlüğe bu modülün değerlerini yazar.</summary>
    /// <param name="stats">Yazılacak sözlük.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task ContributeAsync(IDictionary<string, object> stats, CancellationToken cancellationToken);
}
