using TahaKonsept.Agent.Abstractions.Engines;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp.Abstract;

/// <summary>
/// stable-diffusion.cpp Windows ikilisini (<c>sd-cli.exe</c>) bulur ve gerekirse GitHub'daki son sürümden kurar
/// (NVIDIA varsa CUDA 12 + cudart, yoksa Vulkan, o da yoksa CPU).
/// </summary>
public interface ISdCppInstaller
{
    /// <summary>Kurulum klasörü (<c>ToolsDir/sd-cpp</c>).</summary>
    string InstallDir { get; }

    /// <summary>Kurulu <c>sd-cli.exe</c> yolunu döner; yoksa <c>null</c> (ağ kullanmaz).</summary>
    /// <returns>Yol ya da <c>null</c>.</returns>
    string? FindExecutable();

    /// <summary>Kurulu değilse indirip açar; yolu döner.</summary>
    /// <param name="progress">İlerleme (isteğe bağlı).</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns><c>sd-cli.exe</c> yolu ya da hata.</returns>
    Task<Result<string>> EnsureInstalledAsync(IProgress<EngineSetupProgress>? progress, CancellationToken ct);

    /// <summary>Tercihe ve donanıma göre seçilecek hızlandırıcı.</summary>
    /// <returns>Hızlandırıcı.</returns>
    SdCppBackend ResolveBackend();

    /// <summary>Bir sürümden, hızlandırıcıya göre indirilecek zip dosyalarını seçer (CUDA için ikili + cudart).</summary>
    /// <param name="release">Sürüm.</param>
    /// <param name="backend">Hızlandırıcı.</param>
    /// <returns>Dosyalar; uygun dosya yoksa boş.</returns>
    IReadOnlyList<GitHubReleaseAsset> SelectAssets(GitHubRelease release, SdCppBackend backend);
}
