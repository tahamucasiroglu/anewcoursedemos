namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Kimliksiz sağlık kontrolü cevabı: yalnızca API, veritabanı ve sürüm; kullanıcıya ya da ajana ait
/// bilgi içermez (docs/architecture.md, kural 8). Deploy betikleri bunu kullanır.
/// </summary>
/// <param name="Version">Ürün versiyonu.</param>
/// <param name="Api">API durumu (<c>ok</c>).</param>
/// <param name="Database">Veritabanı durumu (<c>ok</c> ya da <c>down</c>).</param>
public sealed record HealthDto(string Version, string Api, string Database);
