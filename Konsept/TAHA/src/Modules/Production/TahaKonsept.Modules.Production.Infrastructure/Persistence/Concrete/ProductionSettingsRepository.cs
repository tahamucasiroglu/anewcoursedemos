using Microsoft.EntityFrameworkCore;
using TahaKonsept.BuildingBlocks.Infrastructure.Persistence.Base;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Domain.Entities;

namespace TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;

/// <summary>
/// <see cref="IProductionSettingsRepository"/>'nin EF Core uygulaması: kullanıcıya göre tek kayıt.
/// </summary>
/// <param name="context">Modül DbContext'i.</param>
public sealed class ProductionSettingsRepository(ProductionDbContext context) : Repository<ProductionSettings>(context), IProductionSettingsRepository
{
    /// <summary>Kullanıcının kaydı (takipli).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kayıt ya da <c>null</c>.</returns>
    public Task<ProductionSettings?> GetForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Query().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
}
