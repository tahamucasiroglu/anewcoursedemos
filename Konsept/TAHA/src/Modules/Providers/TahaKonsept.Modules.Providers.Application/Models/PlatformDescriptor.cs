namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Anahtar tutulan bir online platformun tanımı (Ayarlar satırı için ad + açıklama + bölge gereksinimi).
/// </summary>
/// <param name="Platform">Platform kimliği.</param>
/// <param name="DisplayName">Görünen ad.</param>
/// <param name="Description">Kullanım alanları.</param>
/// <param name="RequiresRegion">Bölge gerekir mi.</param>
public sealed record PlatformDescriptor(string Platform, string DisplayName, string Description, bool RequiresRegion);
