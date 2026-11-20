namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Bildirimleri okundu yapma (spec 0010, 0010 review F-3): ekranda gösterilen bildirimlerin kimlikleri ve listenin en yeni
/// zamanı (listenin ilk satırının <c>at</c> değeri, olduğu gibi). Yalnızca bunlar okundu olur; arada gelen ya da düzeltilen
/// bildirim okunmamış kalır.
/// </summary>
public sealed record MarkNoticesReadRequest(IReadOnlyList<Guid>? NoticeIds, DateTimeOffset? UpTo);
