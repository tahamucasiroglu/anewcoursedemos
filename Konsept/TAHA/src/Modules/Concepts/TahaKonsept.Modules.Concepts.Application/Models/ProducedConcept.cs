namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// İş içinde başarıyla üretilip kaydedilen tek konsept: kimlik, başlık, maliyet.
/// </summary>
/// <param name="ConceptId">Konsept.</param>
/// <param name="Title">Başlık.</param>
/// <param name="CostUsd">Maliyet (üretim + kelime denetimi).</param>
public sealed record ProducedConcept(Guid ConceptId, string Title, decimal CostUsd);
