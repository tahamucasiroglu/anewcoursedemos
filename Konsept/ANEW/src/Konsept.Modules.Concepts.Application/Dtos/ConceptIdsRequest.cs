namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Onay / onayı geri al isteği (tek konsept ✓ ya da "Seçilenleri onayla").</summary>
/// <param name="Ids">Konseptler.</param>
public sealed record ConceptIdsRequest(IReadOnlyList<Guid>? Ids);
