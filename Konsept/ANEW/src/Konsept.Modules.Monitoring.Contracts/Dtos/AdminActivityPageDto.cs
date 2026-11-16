namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>Aktivitelerin bir sayfası (en yeni üstte, 25'erli).</summary>
/// <param name="Items">Kayıtlar.</param>
/// <param name="Page">Sayfa.</param>
/// <param name="PageSize">Sayfa başına kayıt.</param>
/// <param name="Total">Filtreye uyan toplam kayıt.</param>
/// <param name="Filtered">Bir filtre uygulandı mı (boş durum metni için).</param>
/// <param name="DateFilterIgnored">Başlangıç bitişten sonra olduğu için tarih filtresi uygulanmadı mı.</param>
public sealed record AdminActivityPageDto(IReadOnlyList<AdminActivityDto> Items, int Page, int PageSize, int Total, bool Filtered, bool DateFilterIgnored);
