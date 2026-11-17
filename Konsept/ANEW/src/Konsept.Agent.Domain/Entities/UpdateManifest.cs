namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// İmzalı sürüm listesi (spec 0023 Req 6): yeni ajan sürümü, exe dosyasının adı, SHA-256'sı ve boyutu. Bileşenlerin sürüm/adres/SHA-256
/// listesi exe'ye gömülüdür (0009 <c>components.json</c>); exe'nin hash'i bu imzalı listede olduğu için bileşenler de aynı imzaya bağlıdır.
/// </summary>
/// <param name="Version">Ürün sürümü (ör. <c>0.5.0</c>).</param>
/// <param name="FileName">Exe dosyasının adı (yalnızca ad, klasör yok).</param>
/// <param name="Sha256">Exe'nin SHA-256'sı (küçük harf onaltılık).</param>
/// <param name="Size">Exe boyutu (bayt).</param>
public sealed record UpdateManifest(string Version, string FileName, string Sha256, long Size);
