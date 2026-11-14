using TahaKonsept.Modules.Concepts.Application.Base;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Yedek <see cref="Abstract.INarrationPaceSource"/>: üretim modülü bağlı değilse bilgi vermez (<c>null</c>) — bağlam kurucu
/// dil sabitine (<c>ConceptLimits</c>) düşer. API host'unda Production'ın adaptörü bunun yerine kaydedilir.
/// </summary>
public sealed class DefaultNarrationPaceSource : NarrationPaceSourceBase
{
    /// <summary>Bilgi yok.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="language">Dil.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns><c>null</c>.</returns>
    protected override Task<double?> ResolveAsync(Guid userId, string language, CancellationToken cancellationToken) =>
        Task.FromResult<double?>(null);
}
