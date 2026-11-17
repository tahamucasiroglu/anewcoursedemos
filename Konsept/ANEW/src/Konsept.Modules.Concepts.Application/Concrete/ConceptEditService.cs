using Konsept.Modules.AiGateway.Contracts.Abstract;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;
using Konsept.Modules.Concepts.Contracts.Abstract;
using Konsept.Modules.Profiles.Contracts.Abstract;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Konsept düzenleme döngüsü; davranış <see cref="ConceptEditServiceBase"/>'tedir.</summary>
/// <param name="caller">Model çağrısı.</param>
/// <param name="profiles">Profil bağlamı.</param>
/// <param name="models">Seçilebilir metin modelleri.</param>
/// <param name="concepts">Konsept deposu.</param>
/// <param name="revisions">Revizyon kaydı.</param>
/// <param name="launcher">Arka plan başlatıcı.</param>
/// <param name="rules">Kurallar.</param>
/// <param name="regeneration">Yeniden üret bağlamı.</param>
/// <param name="time">Saat.</param>
public sealed class ConceptEditService(
    IModelCaller caller,
    IProfileGenerationReader profiles,
    ITextModelDirectory models,
    IConceptRepository concepts,
    IConceptRevisionRegistry revisions,
    IConceptJobLauncher launcher,
    IConceptRuleSource rules,
    IConceptRegenerationContext regeneration,
    TimeProvider time) : ConceptEditServiceBase(caller, profiles, models, concepts, revisions, launcher, rules, regeneration, time);
