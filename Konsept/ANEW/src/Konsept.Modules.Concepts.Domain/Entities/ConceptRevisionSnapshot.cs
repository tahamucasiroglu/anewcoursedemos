using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Revizyonun tutarlı anlık görüntüsü.</summary>
/// <param name="Id">Revizyon.</param>
/// <param name="ConceptId">Konsept.</param>
/// <param name="Status">Durum.</param>
/// <param name="Scenes">Sahneler (yalnızca hazırken).</param>
/// <param name="WordRepetitionWarning">Kelime tekrarı uyarısı.</param>
/// <param name="DurationMismatchWarning">Süre uyumsuz uyarısı.</param>
/// <param name="Error">Hata mesajı (başarısızsa).</param>
public sealed record ConceptRevisionSnapshot(
    Guid Id,
    Guid ConceptId,
    RevisionStatus Status,
    IReadOnlyList<ConceptScene> Scenes,
    bool WordRepetitionWarning,
    bool DurationMismatchWarning,
    string? Error);
