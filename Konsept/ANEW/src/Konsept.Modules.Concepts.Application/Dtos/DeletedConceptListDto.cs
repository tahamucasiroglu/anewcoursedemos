namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Silinenler'in Konseptler sekmesi (50'şerli, en yeni silinen üstte; G-14).</summary>
/// <param name="Items">Satırlar.</param>
/// <param name="Page">Sayfa (1'den).</param>
/// <param name="PageSize">Sayfa boyutu.</param>
/// <param name="Total">Toplam.</param>
public sealed record DeletedConceptListDto(IReadOnlyList<DeletedConceptItemDto> Items, int Page, int PageSize, int Total);
