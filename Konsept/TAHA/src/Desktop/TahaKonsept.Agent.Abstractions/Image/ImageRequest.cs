namespace TahaKonsept.Agent.Abstractions.Image;

/// <summary>Görsel üretim isteği.</summary>
/// <param name="ModelKey">Model anahtarı (ör. <c>z-image-turbo</c>).</param>
/// <param name="Prompt">Pozitif istem (görselde yazı istenmez).</param>
/// <param name="NegativePrompt">Negatif istem.</param>
/// <param name="Width">Genişlik.</param>
/// <param name="Height">Yükseklik.</param>
/// <param name="Seed">Tohum (tekrarlanabilirlik).</param>
/// <param name="Steps">Adım sayısı (<c>null</c> = model varsayılanı).</param>
/// <param name="OutputPath">Çıktı .png.</param>
public sealed record ImageRequest(string ModelKey, string Prompt, string? NegativePrompt, int Width, int Height, long Seed, int? Steps, string OutputPath);
