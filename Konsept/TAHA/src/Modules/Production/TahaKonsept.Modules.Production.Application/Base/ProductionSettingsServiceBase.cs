using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IProductionSettingsService"/> ortak davranışı: kayıt yoksa varsayılanlar döner; kaydederken <see cref="ProductionSettingsDefaults.Normalize"/>
/// uygulanır ve JSON olarak saklanır.
/// </summary>
public abstract class ProductionSettingsServiceBase : IProductionSettingsService
{
    private readonly IProductionSettingsRepository _repository;
    private readonly ICurrentUser _currentUser;

    /// <summary>Oluşturur.</summary>
    /// <param name="repository">Ayar deposu.</param>
    /// <param name="currentUser">Oturumdaki kullanıcı.</param>
    protected ProductionSettingsServiceBase(IProductionSettingsRepository repository, ICurrentUser currentUser)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Oturumdaki kullanıcının ayarları.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar.</returns>
    public async Task<Result<ProductionSettingsDto>> GetAsync(CancellationToken cancellationToken) =>
        await GetForUserAsync(_currentUser.GetRequiredUserId(), cancellationToken);

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Kaydedilen ayarlar.</returns>
    public async Task<Result<ProductionSettingsDto>> UpdateAsync(ProductionSettingsDto settings, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var normalized = ProductionSettingsDefaults.Normalize(settings);
        var json = ProductionJson.Write(normalized);
        var entity = await _repository.GetForUserAsync(userId, cancellationToken);
        if (entity is null)
        {
            await _repository.AddAsync(ProductionSettings.Create(userId, json), cancellationToken);
        }
        else
        {
            entity.Update(json);
        }

        await _repository.UnitOfWork.SaveChangesAsync(cancellationToken);
        return normalized;
    }

    /// <summary>Kullanıcının ayarları (varsayılanlarla).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ayarlar.</returns>
    public async Task<ProductionSettingsDto> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetForUserAsync(userId, cancellationToken);
        return ProductionSettingsDefaults.Normalize(entity is null ? null : ProductionJson.Read<ProductionSettingsDto>(entity.SettingsJson));
    }
}
