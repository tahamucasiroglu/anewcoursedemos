namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Seçilen video (spec 0007 Req 2, 5, 6): tüm sürümler (eskiden yeniye) ve sürümlerin toplam maliyeti.</summary>
/// <param name="Id">Video (konsept).</param>
/// <param name="Title">Başlık (en son sürüm).</param>
/// <param name="TotalCostUsd">Tüm sürümlerin toplam online maliyeti (USD).</param>
/// <param name="Versions">Sürümler.</param>
public sealed record ProducedVideoDetailDto(Guid Id, string Title, decimal TotalCostUsd, IReadOnlyList<ProducedVersionDto> Versions);
