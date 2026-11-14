namespace TahaKonsept.Agent.Engines.Image.Options;

/// <summary>stable-diffusion.cpp ikilisi için hızlandırıcı tercihi. <see cref="Auto"/>: NVIDIA varsa CUDA, yoksa Vulkan, o da yoksa CPU.</summary>
public enum SdCppBackendPreference
{
    /// <summary>Donanıma göre otomatik seç.</summary>
    Auto,

    /// <summary>CUDA 12 derlemesi (NVIDIA).</summary>
    Cuda,

    /// <summary>Vulkan derlemesi (AMD/Intel/NVIDIA).</summary>
    Vulkan,

    /// <summary>Yalnızca CPU (çok yavaş).</summary>
    Cpu,
}
