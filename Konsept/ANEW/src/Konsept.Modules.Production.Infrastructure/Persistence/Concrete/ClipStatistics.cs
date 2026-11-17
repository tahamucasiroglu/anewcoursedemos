using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// Klip istatistiği (spec 0010 Req 3): <c>production.videos</c> sürüm kayıtlarından konsept (video) başına ilk tamamlanma anı; aynı
/// videonun yeni sürümleri ayrı klip sayılmaz. Silinmiş (Silinenler'deki) sürümler sayılır; kalıcı silinen videolar klip izinden
/// (<c>production.clip_tombstones</c>) okunur — silme sayıyı düşürmez (spec 0019, B-130). İçerik (başlık, konsept metni) okunmaz.
/// </summary>
public sealed class ClipStatistics : EfRepositoryBase<ProductionDbContext>, IClipStatistics
{
    /// <summary>Initializes a new instance of the <see cref="ClipStatistics"/> class.</summary>
    /// <param name="context">Production veritabanı bağlamı.</param>
    public ClipStatistics(ProductionDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DateTimeOffset>> ListFirstCompletionsAsync(CancellationToken cancellationToken)
    {
        var live = await Context.Videos
            .AsNoTracking()
            .GroupBy(v => v.ConceptId)
            .Select(g => new { ConceptId = g.Key, At = g.Min(v => v.EndedAt) })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var gone = await Context.ClipTombstones
            .AsNoTracking()
            .Select(t => new { t.ConceptId, At = t.FirstCompletedAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Konsept başına tek klip: hem canlı satır hem iz varsa en eski an.
        return [.. live.Concat(gone).GroupBy(c => c.ConceptId).Select(g => g.Min(c => c.At))];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> CountByUserAsync(CancellationToken cancellationToken) =>
        await Context.Videos
            .AsNoTracking()
            .GroupBy(v => v.UserId)
            .Select(g => new { UserId = g.Key, Clips = g.Select(v => v.ConceptId).Distinct().Count() })
            .ToDictionaryAsync(r => r.UserId, r => r.Clips, cancellationToken)
            .ConfigureAwait(false);
}
