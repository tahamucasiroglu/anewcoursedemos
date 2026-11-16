namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>Model çıktısından ayrıştırılmış, alanları eksiksiz konsept (spec 0014 Req 10, AC-13).</summary>
/// <param name="Title">Başlık.</param>
/// <param name="Scenes">Sahneler (en az bir).</param>
public sealed record ParsedConcept(string Title, IReadOnlyList<ConceptScene> Scenes);
