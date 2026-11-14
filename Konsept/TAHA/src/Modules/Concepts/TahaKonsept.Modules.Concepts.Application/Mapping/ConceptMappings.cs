using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Mapping;

/// <summary>
/// Elle eşlemeler (AutoMapper yok): <see cref="ToSummary"/>, <see cref="ToDetail"/> (entity → DTO) ve
/// <see cref="ToLlm"/> (belge → düzeltme promptuna giden kısa LLM şekli).
/// </summary>
public static class ConceptMappings
{
    /// <summary>Liste öğesine çevirir.</summary>
    /// <param name="concept">Konsept.</param>
    /// <returns>DTO.</returns>
    public static ConceptSummaryDto ToSummary(this Concept concept)
    {
        ArgumentNullException.ThrowIfNull(concept);
        var doc = concept.Document;
        var estimated = (int)Math.Round(doc.Scenes.Sum(s => s.DurationHintSec ?? 0));
        return new ConceptSummaryDto
        {
            Id = concept.Id,
            ProfileId = concept.ProfileId,
            Title = concept.Title,
            Summary = doc.Summary,
            Status = concept.Status,
            Origin = concept.Origin,
            SceneCount = doc.Scenes.Count,
            EstimatedDurationSec = estimated > 0 ? estimated : doc.TargetDurationSec,
            Tags = doc.Tags,
            CostUsd = concept.CostUsd,
            CreatedAt = concept.CreatedAtUtc,
            UpdatedAt = concept.UpdatedAtUtc,
            ApprovedAt = concept.ApprovedAtUtc,
            DeletedAt = concept.DeletedAtUtc,
        };
    }

    /// <summary>Ayrıntıya çevirir.</summary>
    /// <param name="concept">Konsept.</param>
    /// <returns>DTO.</returns>
    public static ConceptDetailDto ToDetail(this Concept concept)
    {
        ArgumentNullException.ThrowIfNull(concept);
        return new ConceptDetailDto
        {
            Id = concept.Id,
            ProfileId = concept.ProfileId,
            Title = concept.Title,
            Status = concept.Status,
            Origin = concept.Origin,
            Document = concept.Document,
            ModelKey = concept.ModelKey,
            InputTokens = concept.InputTokens,
            OutputTokens = concept.OutputTokens,
            CostUsd = concept.CostUsd,
            RevisionOfId = concept.RevisionOfId,
            CreatedAt = concept.CreatedAtUtc,
            UpdatedAt = concept.UpdatedAtUtc,
            ApprovedAt = concept.ApprovedAtUtc,
            DeletedAt = concept.DeletedAtUtc,
        };
    }

    /// <summary>Belgeyi LLM şekline çevirir (kanal alanları olmadan; düzeltme promptu için).</summary>
    /// <param name="document">Belge.</param>
    /// <returns>LLM şekli.</returns>
    public static ConceptLlmOutput ToLlm(this ConceptDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new ConceptLlmOutput
        {
            Title = document.Title,
            Hook = document.Hook,
            Summary = document.Summary,
            Scenes = document.Scenes.Select(s => new ConceptSceneLlm
            {
                Title = s.Title,
                OnScreen = s.OnScreen,
                VisualPrompt = s.VisualPrompt,
                Narration = s.Narration,
                DurationHintSec = s.DurationHintSec ?? 0,
            }).ToList(),
            Sources = document.Sources.Select(s => new ConceptSourceLlm { Title = s.Title, Url = s.Url }).ToList(),
            Pronunciations = document.Pronunciations.Select(p => new PronunciationLlm { Written = p.Written, Spoken = p.Spoken }).ToList(),
            Tags = document.Tags.ToList(),
        };
    }
}
