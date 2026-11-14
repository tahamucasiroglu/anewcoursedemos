namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp;

/// <summary>Seçilen bellek kipi ve sd-cli'ye eklenecek bayraklar.</summary>
/// <param name="Level">Kip.</param>
/// <param name="Arguments">Bellek bayrakları (model/istem argümanlarından sonra, çıktıdan önce eklenir).</param>
/// <param name="FreeVramMiB">Kararda kullanılan boş VRAM (bilinmiyorsa <c>null</c>).</param>
public sealed record SdCppMemoryPlan(SdCppMemoryLevel Level, IReadOnlyList<string> Arguments, int? FreeVramMiB);
