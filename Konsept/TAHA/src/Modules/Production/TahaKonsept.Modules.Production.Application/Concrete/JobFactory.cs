using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Base;
using TahaKonsept.Modules.Profiles.Application.Abstract;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Varsayılan iş fabrikası (scoped).
/// </summary>
/// <param name="settings">Ayar servisi.</param>
/// <param name="concepts">Konsept portu.</param>
/// <param name="profiles">Profil okuyucu.</param>
/// <param name="jobs">İş deposu.</param>
/// <param name="contexts">Bağlam fabrikası.</param>
/// <param name="handlers">Aşama işleyicileri.</param>
/// <param name="reuse">Yeniden kullanım planlayıcı.</param>
/// <param name="suggestions">Okunuş önerileri.</param>
public sealed class JobFactory(
    IProductionSettingsService settings,
    IConceptSource concepts,
    IProfileReader profiles,
    IJobRepository jobs,
    IPipelineContextFactory contexts,
    IStageHandlerRegistry handlers,
    IReusePlanner reuse,
    IPronunciationSuggestions suggestions)
    : JobFactoryBase(settings, concepts, profiles, jobs, contexts, handlers, reuse, suggestions);
