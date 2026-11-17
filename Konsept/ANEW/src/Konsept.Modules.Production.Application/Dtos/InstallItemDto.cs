namespace Konsept.Modules.Production.Application.Dtos;

/// <summary>İndirilecek bileşen (spec 0027): kimlik, ad, yaklaşık indirme ve kurulu boyut (bayt).</summary>
/// <param name="Id">Kimlik.</param>
/// <param name="Name">Ad.</param>
/// <param name="DownloadBytes">İndirme boyutu.</param>
/// <param name="InstalledBytes">Kurulu boyut.</param>
/// <param name="License">Lisans ya da <c>null</c> (spec 0028).</param>
public sealed record InstallItemDto(string Id, string Name, long DownloadBytes, long InstalledBytes, string? License = null);
