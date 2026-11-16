namespace Konsept.Agent.Domain.Entities;

/// <summary>Bir sahnenin kısıtlı render isteği (spec 0018): sahne klasöründe <c>index.html</c> hazırdır.</summary>
/// <param name="JobDirectory">İş klasörü (konsept başına; yazılabilir tek yer).</param>
/// <param name="SceneDirectory">Sahne klasörü (iş klasörünün içinde).</param>
/// <param name="Width">Genişlik (konseptin biçimi).</param>
/// <param name="Height">Yükseklik.</param>
/// <param name="DurationSeconds">Beklenen süre.</param>
/// <param name="ClipName">Klip dosyasının adı (<c>clip.mp4</c> ya da NVENC yoksa <c>clip.webm</c>).</param>
public sealed record SceneRenderRequest(string JobDirectory, string SceneDirectory, int Width, int Height, double DurationSeconds, string ClipName);
