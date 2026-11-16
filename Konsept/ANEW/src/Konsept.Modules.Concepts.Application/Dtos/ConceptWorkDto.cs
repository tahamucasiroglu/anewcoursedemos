namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Profilin üretim durumu: hazır olma, bekleyen öneriler ve iki şeridin son işi (bu profile aitse).</summary>
/// <param name="ProfileId">Profil.</param>
/// <param name="Readiness">Hazır olma.</param>
/// <param name="Suggestions">Bekleyen öneriler.</param>
/// <param name="TopicJob">Konudan üret şeridinin işi.</param>
/// <param name="IdeaJob">Fikirden üret şeridinin işi.</param>
public sealed record ConceptWorkDto(
    Guid ProfileId,
    ReadinessDto Readiness,
    IReadOnlyList<SuggestionDto> Suggestions,
    ConceptJobDto? TopicJob,
    ConceptJobDto? IdeaJob);
