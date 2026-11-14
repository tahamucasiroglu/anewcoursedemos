namespace TahaKonsept.Shared.Contracts.Concepts;

/// <summary>
/// Düzeltme adayı (henüz kaydedilmedi): <c>POST api/concepts/{id}/candidates/{candidateId}/accept</c> ile konseptin yerine geçer,
/// <c>DELETE api/concepts/{id}/candidates/{candidateId}</c> ile atılır. Adaylar sunucu belleğinde bir süre (2 saat) tutulur.
/// </summary>
public sealed record ConceptRevisionCandidateDto
{
    /// <summary>Aday kimliği.</summary>
    public Guid CandidateId { get; init; }

    /// <summary>Konsept kimliği.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Verilen talimat.</summary>
    public string Instruction { get; init; } = string.Empty;

    /// <summary>Düzeltilmiş belge.</summary>
    public ConceptDocument Document { get; init; } = new();

    /// <summary>Bu düzeltmenin maliyeti ($).</summary>
    public decimal CostUsd { get; init; }

    /// <summary>Oluşturulma anı (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
