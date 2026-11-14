using TahaKonsept.Agent.Abstractions.Image;
using TahaKonsept.Agent.Engines.Image.Catalog;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;

/// <summary><c>sd-cli.exe</c> komut satırı argümanlarını model tanımı ve istekten üretir (tek tek argüman; kaçış süreç çalıştırıcıda).</summary>
public interface ISdCppArgumentBuilder
{
    /// <summary>Argüman listesini üretir.</summary>
    /// <param name="model">Model tanımı.</param>
    /// <param name="resolvePath">Katalog dosyasını yerel tam yola çevirir.</param>
    /// <param name="request">İstek.</param>
    /// <param name="lowVram">Düşük VRAM bayrakları eklensin mi.</param>
    /// <returns>Argümanlar.</returns>
    IReadOnlyList<string> Build(ImageModelDefinition model, Func<ImageModelFile, string> resolvePath, ImageRequest request, bool lowVram);
}
