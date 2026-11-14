using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Providers.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan konuşma hızı servisi (scoped); davranış <see cref="SpeechRateServiceBase"/>'tedir.
/// </summary>
/// <param name="stats">Ölçüm deposu.</param>
/// <param name="settings">Üretim ayarları servisi.</param>
/// <param name="catalog">Model kataloğu.</param>
public sealed class SpeechRateService(ISpeechRateStatRepository stats, IProductionSettingsService settings, IModelCatalog catalog)
    : SpeechRateServiceBase(stats, settings, catalog);
