using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan yeniden kullanım planlayıcısı (scoped; işleyici kayıt defterine bağlı).
/// </summary>
/// <param name="handlers">Aşama işleyicileri.</param>
/// <param name="hasher">Hash'leyici.</param>
/// <param name="planner">Sahne planlayıcı.</param>
public sealed class ReusePlanner(IStageHandlerRegistry handlers, IStageInputHasher hasher, IScenePlanner planner)
    : ReusePlannerBase(handlers, hasher, planner);
