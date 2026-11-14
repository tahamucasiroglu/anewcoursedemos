using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// DI'daki tüm <see cref="IStageHandler"/> kayıtlarından kurulan aşama sözlüğü (scoped; işleyiciler scoped geçitlere bağlıdır).
/// </summary>
/// <param name="handlers">İşleyiciler.</param>
public sealed class StageHandlerRegistry(IEnumerable<IStageHandler> handlers) : StageHandlerRegistryBase(handlers);
