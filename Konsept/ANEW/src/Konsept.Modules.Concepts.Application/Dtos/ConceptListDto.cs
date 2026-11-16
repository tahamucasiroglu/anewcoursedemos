namespace Konsept.Modules.Concepts.Application.Dtos;

/// <summary>Üretilenler sayfası (50'şerli, en yeni üstte; G-14).</summary>
/// <param name="Items">Satırlar.</param>
/// <param name="Page">Sayfa (1'den).</param>
/// <param name="PageSize">Sayfa boyutu.</param>
/// <param name="Total">Filtreye uyan toplam.</param>
/// <param name="Statuses">Uygulanan durum kodları.</param>
public sealed record ConceptListDto(IReadOnlyList<ConceptListItemDto> Items, int Page, int PageSize, int Total, IReadOnlyList<string> Statuses);
