namespace TahaKonsept.Agent.Engines.Image.StableDiffusionCpp;

/// <summary>Kurulu/kurulacak stable-diffusion.cpp derlemesinin hızlandırıcısı.</summary>
public enum SdCppBackend
{
    /// <summary>CUDA 12 (NVIDIA); <c>cudart</c> paketiyle birlikte.</summary>
    Cuda,

    /// <summary>Vulkan.</summary>
    Vulkan,

    /// <summary>Yalnız CPU.</summary>
    Cpu,
}
