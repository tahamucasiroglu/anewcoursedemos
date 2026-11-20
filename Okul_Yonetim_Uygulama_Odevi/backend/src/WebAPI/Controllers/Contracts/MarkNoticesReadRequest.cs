namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Bildirimleri okundu yapma (spec 0010, 0010 review F-3 ve review 2): listenin en yeni zamanı (listenin ilk satırının
/// <c>at</c> değeri, olduğu gibi). Bu zamana kadar değişen bütün okunmamışlar okundu olur (listede gösterilmeyen eskiler de);
/// arada gelen ya da düzeltilen bildirim okunmamış kalır.
/// </summary>
public sealed record MarkNoticesReadRequest(DateTimeOffset? UpTo);
