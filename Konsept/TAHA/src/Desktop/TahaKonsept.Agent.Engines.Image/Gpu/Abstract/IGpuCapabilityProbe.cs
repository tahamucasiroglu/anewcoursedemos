namespace TahaKonsept.Agent.Engines.Image.Gpu.Abstract;

/// <summary>Hangi sd.cpp derlemesinin indirileceğine karar vermek için hızlı (süreç başlatmayan) GPU yeteneği yoklaması.</summary>
public interface IGpuCapabilityProbe
{
    /// <summary>NVIDIA sürücüsü (CUDA çalışma zamanı, <c>nvcuda.dll</c>) kurulu mu.</summary>
    bool HasNvidiaDriver { get; }

    /// <summary>Vulkan yükleyicisi (<c>vulkan-1.dll</c>) kurulu mu.</summary>
    bool HasVulkanRuntime { get; }
}
