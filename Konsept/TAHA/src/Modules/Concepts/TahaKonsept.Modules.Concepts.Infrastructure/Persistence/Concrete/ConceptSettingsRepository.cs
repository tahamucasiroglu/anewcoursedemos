using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Concepts.Application.Abstract;
using TahaKonsept.Modules.Concepts.Domain.Entities;

namespace TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IConceptSettingsRepository"/>'nin EF Core uygulaması.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class ConceptSettingsRepository(ConceptsDbContext context) : Repository<ConceptSettings>(context), IConceptSettingsRepository
{
    /// <summary>Kullanıcının ayarları (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar ya da null.</returns>
    public Task<ConceptSettings?> GetForUserTrackedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Query().FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    /// <summary>Kullanıcının ayarları (takipsiz).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar ya da null.</returns>
    public Task<ConceptSettings?> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        QueryNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
}
