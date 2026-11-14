namespace TahaKonsept.Modules.Providers.Application.Models;

/// <summary>
/// Bir görsel sağlayıcısının ham çıktısı: görsel baytları, MIME türü ve ölçülen kullanım.
/// </summary>
/// <param name="Images">Görseller (her biri bir dosya içeriği).</param>
/// <param name="MimeType">MIME türü (ör. <c>image/png</c>).</param>
/// <param name="Usage">Kullanım (genelde <see cref="CallUsage.Images"/>).</param>
public sealed record ImageModelOutput(IReadOnlyList<byte[]> Images, string MimeType, CallUsage Usage);
