using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;
using Konsept.Modules.Profiles.Contracts.Abstract;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Konseptler sayfasının üretim alanları; davranış <see cref="ConceptWorkServiceBase"/>'tedir.</summary>
/// <param name="caller">Model çağrısı.</param>
/// <param name="profiles">Profil bağlamı.</param>
/// <param name="models">Seçilebilir modeller.</param>
/// <param name="suggestions">Öneri deposu.</param>
/// <param name="registry">İş kaydı.</param>
/// <param name="launcher">Başlatıcı.</param>
/// <param name="time">Saat.</param>
public sealed class ConceptWorkService(
    IModelCaller caller,
    IProfileGenerationReader profiles,
    ITextModelDirectory models,
    ITopicSuggestionRepository suggestions,
    IConceptJobRegistry registry,
    IConceptJobLauncher launcher,
    TimeProvider time) : ConceptWorkServiceBase(caller, profiles, models, suggestions, registry, launcher, time);
