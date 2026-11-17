namespace Konsept.Modules.Production.Domain.Entities;

/// <summary>İndirme onayı bekleyen bir bileşen (spec 0027): kimlik, görünen ad, yaklaşık indirme ve kurulu boyut (bayt).</summary>
/// <param name="Id">Bileşen kimliği.</param>
/// <param name="Name">Görünen ad.</param>
/// <param name="DownloadBytes">Yaklaşık indirme boyutu.</param>
/// <param name="InstalledBytes">Yaklaşık kurulu boyut.</param>
/// <param name="License">Lisans adı (spec 0028 Req 12) ya da <c>null</c>.</param>
public sealed record InstallItem(string Id, string Name, long DownloadBytes, long InstalledBytes, string? License = null);
