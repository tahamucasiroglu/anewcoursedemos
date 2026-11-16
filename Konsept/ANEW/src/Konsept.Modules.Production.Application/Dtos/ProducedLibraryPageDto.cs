namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Üretilen Videolar listesinin bir parçası (spec 0007 Req 1): en çok 50 öğe ve devamı olup olmadığı.</summary>
/// <param name="Items">Öğeler, üretim tarihine göre en yeni üstte.</param>
/// <param name="HasMore">"Daha fazla göster" gösterilir mi.</param>
public sealed record ProducedLibraryPageDto(IReadOnlyList<ProducedLibraryItemDto> Items, bool HasMore);
