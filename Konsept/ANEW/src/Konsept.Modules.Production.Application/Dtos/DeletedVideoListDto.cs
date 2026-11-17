namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>Silinenler'in Videolar sekmesi (spec 0019 Req 3; 50'şerli, en yeni silinen üstte — 0016 ile aynı biçim).</summary>
/// <param name="Items">Satırlar.</param>
/// <param name="Page">Sayfa (1'den).</param>
/// <param name="PageSize">Sayfa boyutu.</param>
/// <param name="Total">Toplam.</param>
public sealed record DeletedVideoListDto(IReadOnlyList<DeletedVideoItemDto> Items, int Page, int PageSize, int Total);
