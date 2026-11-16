using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Profiles.Contracts.Dtos;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Yeniden üretim işinin girdisi; istekte sunucuda kurulur (çağıran, profilin o anki bağlamı, konsept, taban sahneler, talimat).</summary>
/// <param name="Caller">Çağıran.</param>
/// <param name="Context">Profil bağlamı.</param>
/// <param name="Concept">Konsept.</param>
/// <param name="BaseScenes">Modele gidecek sahneler.</param>
/// <param name="Instruction">Düzenleme talimatı.</param>
public sealed record RevisionInput(
    ModelCallerIdentity Caller,
    ProfileGenerationContext Context,
    Concept Concept,
    IReadOnlyList<ConceptScene> BaseScenes,
    string Instruction);
