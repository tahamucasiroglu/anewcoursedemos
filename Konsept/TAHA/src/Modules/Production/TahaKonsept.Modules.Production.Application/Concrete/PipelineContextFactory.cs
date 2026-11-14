using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan bağlam fabrikası (scoped).
/// </summary>
/// <param name="pronunciations">Okunuş sözlüğü deposu.</param>
/// <param name="planner">Sahne planlayıcı.</param>
public sealed class PipelineContextFactory(IPronunciationRepository pronunciations, IScenePlanner planner)
    : PipelineContextFactoryBase(pronunciations, planner);
