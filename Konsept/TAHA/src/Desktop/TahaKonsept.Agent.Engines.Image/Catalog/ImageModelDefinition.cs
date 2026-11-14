namespace TahaKonsept.Agent.Engines.Image.Catalog;

/// <summary>
/// Katalogdaki bir yerel görsel modeli: anahtar, ad, lisans, en az VRAM, varsayılan örnekleme ayarları ve dosya listesi.
/// stable-diffusion.cpp argümanları bu tanımdan üretilir.
/// </summary>
/// <param name="Key">Model anahtarı (ör. <c>z-image-turbo</c>).</param>
/// <param name="DisplayName">Kullanıcıya gösterilen ad.</param>
/// <param name="Family">Mimari ailesi (<c>z-image</c>, <c>flux2</c>, <c>flux3</c>, <c>sd3</c>, <c>sdxl</c>, <c>qwen-image</c>).</param>
/// <param name="License">Lisans adı.</param>
/// <param name="MinVramMb">Önerilen en az VRAM (MB).</param>
/// <param name="DefaultSteps">Varsayılan adım sayısı.</param>
/// <param name="CfgScale">CFG ölçeği.</param>
/// <param name="SamplingMethod">Örnekleyici (<c>null</c> = sd.cpp varsayılanı).</param>
/// <param name="Scheduler">Zamanlayıcı (<c>null</c> = sd.cpp varsayılanı).</param>
/// <param name="FlowShift">Flow shift (<c>null</c> = otomatik).</param>
/// <param name="ExtraArgs">Modele özgü ek CLI argümanları.</param>
/// <param name="Files">İndirilecek dosyalar.</param>
public sealed record ImageModelDefinition(
    string Key,
    string DisplayName,
    string Family,
    string License,
    int MinVramMb,
    int DefaultSteps,
    double CfgScale,
    string? SamplingMethod,
    string? Scheduler,
    double? FlowShift,
    IReadOnlyList<string> ExtraArgs,
    IReadOnlyList<ImageModelFile> Files)
{
    /// <summary>Tüm dosyaların toplam boyutu (bayt).</summary>
    public long TotalSizeBytes => Files.Sum(f => f.SizeBytes);
}
