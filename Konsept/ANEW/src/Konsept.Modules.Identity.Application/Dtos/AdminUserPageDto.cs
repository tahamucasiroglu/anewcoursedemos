namespace Konsept.Modules.Identity.Application.Dtos;

/// <summary>Admin kullanıcı listesinin bir sayfası (spec 0024 Req 1–2).</summary>
/// <param name="Items">Satırlar.</param>
/// <param name="Page">Sayfa.</param>
/// <param name="PageSize">Sayfa boyutu (25).</param>
/// <param name="Total">Filtreye uyan toplam.</param>
/// <param name="Filtered">Arama ya da durum filtresi uygulandı mı (boş durum metni için).</param>
public sealed record AdminUserPageDto(IReadOnlyList<AdminUserDto> Items, int Page, int PageSize, int Total, bool Filtered);
