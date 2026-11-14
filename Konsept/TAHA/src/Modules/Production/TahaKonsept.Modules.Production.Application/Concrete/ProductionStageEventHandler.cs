using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Ajan aşama olaylarını üretim işlerine uygulayan işleyici (scoped; <c>IAgentStageEventHandler</c> olarak <c>Replace</c> ile kaydedilir).
/// </summary>
/// <param name="dependencies">Bağımlılıklar.</param>
public sealed class ProductionStageEventHandler(StageEventHandlerDependencies dependencies) : ProductionStageEventHandlerBase(dependencies);
