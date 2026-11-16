using Konsept.Modules.AiGateway.Contracts.Dtos;
using Konsept.Modules.Concepts.Domain.Entities;
using Konsept.Modules.Profiles.Contracts.Dtos;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>
/// Arka plan işinin girdisi; başlatma isteğinde sunucuda kurulur: çağıran (kullanıcı + yetkiler), profil bağlamı (o anki değerler, Req 27),
/// adet, fikir (fikirden üretim) ve seçilen öneriler (Seçtiklerini üret).
/// </summary>
/// <param name="Caller">Çağıran.</param>
/// <param name="Context">Profil bağlamı.</param>
/// <param name="Count">Adet.</param>
/// <param name="Idea">Fikir.</param>
/// <param name="Topics">Seçilen öneriler.</param>
public sealed record ConceptJobInput(
    ModelCallerIdentity Caller,
    ProfileGenerationContext Context,
    int Count,
    string? Idea,
    IReadOnlyList<TopicSuggestion> Topics);
