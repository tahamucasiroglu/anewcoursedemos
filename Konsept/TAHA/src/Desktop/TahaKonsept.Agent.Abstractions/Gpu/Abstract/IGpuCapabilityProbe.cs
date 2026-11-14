namespace TahaKonsept.Agent.Abstractions.Gpu.Abstract;

/// <summary>
/// Hızlı (süreç başlatmayan) GPU yeteneği yoklaması: sd.cpp derleme seçimi (CUDA/Vulkan/CPU) ve yerel ses motorlarının cihaz seçimi
/// (torch CUDA tekerlekleri + <c>cuda</c>) bu bilgiye dayanır. Önemli alanlar: <see cref="HasNvidiaDriver"/>, <see cref="HasVulkanRuntime"/>.
/// </summary>
public interface IGpuCapabilityProbe
{
    /// <summary>NVIDIA sürücüsü (CUDA çalışma zamanı, <c>nvcuda.dll</c>) kurulu mu.</summary>
    bool HasNvidiaDriver { get; }

    /// <summary>Vulkan yükleyicisi (<c>vulkan-1.dll</c>) kurulu mu.</summary>
    bool HasVulkanRuntime { get; }
}
