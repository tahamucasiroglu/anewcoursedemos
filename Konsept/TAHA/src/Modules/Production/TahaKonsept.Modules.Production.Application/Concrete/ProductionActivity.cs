using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan Production işlem günlüğü yazıcısı (singleton güvenli; <see cref="IActivityLogger"/> kendi kapsamında yazar).
/// </summary>
/// <param name="logger">İşlem günlüğü.</param>
public sealed class ProductionActivity(IActivityLogger logger) : ProductionActivityBase(logger);
