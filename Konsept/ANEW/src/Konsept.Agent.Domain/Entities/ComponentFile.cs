namespace Konsept.Agent.Domain.Entities;

/// <summary>
/// Çok dosyalı bileşenin bir dosyası (spec 0028 Req 3): bileşen klasörüne göre yol, sabit sürümlü (commit) resmi adres, SHA-256 ve boyut.
/// Model ağırlıkları bu biçimle iner; her dosya ayrı doğrulanır.
/// </summary>
/// <param name="Path">Bileşen klasörüne göre göreli yol (ör. <c>onnx/vocoder.onnx</c>).</param>
/// <param name="Url">Sabit sürümlü indirme adresi.</param>
/// <param name="Sha256">Beklenen SHA-256 (küçük harf onaltılık).</param>
/// <param name="Bytes">Beklenen boyut.</param>
public sealed record ComponentFile(string Path, Uri Url, string Sha256, long Bytes);
