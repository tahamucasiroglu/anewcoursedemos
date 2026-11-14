namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// İşin üretim planı: üretilecek girdiler ve planlama maliyeti (fikirden açı üretimi).
/// </summary>
/// <param name="Briefs">Girdiler.</param>
/// <param name="CostUsd">Planlama maliyeti.</param>
public sealed record BriefPlan(IReadOnlyList<ConceptBrief> Briefs, decimal CostUsd);
