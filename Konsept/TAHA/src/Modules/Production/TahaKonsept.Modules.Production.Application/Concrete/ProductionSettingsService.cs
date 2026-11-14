using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan üretim ayarları servisi (scoped).
/// </summary>
/// <param name="repository">Ayar deposu.</param>
/// <param name="currentUser">Oturumdaki kullanıcı.</param>
public sealed class ProductionSettingsService(IProductionSettingsRepository repository, ICurrentUser currentUser)
    : ProductionSettingsServiceBase(repository, currentUser);
