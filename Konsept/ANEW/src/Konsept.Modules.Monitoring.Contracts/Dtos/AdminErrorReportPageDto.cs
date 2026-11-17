namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Admin hata bildirimi listesinin bir sayfası (en yeni üstte, 25'erli; spec 0025 Req 1, 3).</summary>
/// <param name="Items">Satırlar.</param>
/// <param name="Page">Sayfa.</param>
/// <param name="PageSize">Sayfa başına kayıt.</param>
/// <param name="Total">Filtreye uyan toplam kayıt.</param>
/// <param name="Filtered">Varsayılandan (Tümü kaynak + Açık) farklı bir filtre uygulandı mı (boş durum metni için).</param>
/// <param name="Source">Uygulanan kaynak filtresi.</param>
/// <param name="Status">Uygulanan durum filtresi.</param>
public sealed record AdminErrorReportPageDto(IReadOnlyList<AdminErrorReportRowDto> Items, int Page, int PageSize, int Total, bool Filtered, string Source, string Status);
