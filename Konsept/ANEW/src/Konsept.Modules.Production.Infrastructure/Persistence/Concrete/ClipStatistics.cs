using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Shared.Infrastructure.Persistence.Base;
using Microsoft.EntityFrameworkCore;

namespace Konsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// Klip istatistiği (spec 0010 Req 3): <c>production.videos</c> sürüm kayıtlarından konsept (video) başına ilk tamamlanma anı; aynı
/// videonun yeni sürümleri ayrı klip sayılmaz. İçerik (başlık, konsept metni) okunmaz.
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
    public async Task<IReadOnlyList<DateTimeOffset>> ListFirstCompletionsAsync(CancellationToken cancellationToken) =>
        await Context.Videos
            .AsNoTracking()
            .GroupBy(v => v.ConceptId)
            .Select(g => g.Min(v => v.EndedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
