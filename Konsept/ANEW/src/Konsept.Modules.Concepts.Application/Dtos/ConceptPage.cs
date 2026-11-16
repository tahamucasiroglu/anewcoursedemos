using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Depodan bir konsept sayfası ve filtreye uyan toplam.</summary>
/// <param name="Items">Konseptler.</param>
/// <param name="Total">Toplam.</param>
public sealed record ConceptPage(IReadOnlyList<Concept> Items, int Total);
