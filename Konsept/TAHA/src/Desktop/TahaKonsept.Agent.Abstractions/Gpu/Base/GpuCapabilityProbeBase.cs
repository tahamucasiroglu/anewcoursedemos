using TahaKonsept.Agent.Abstractions.Gpu.Abstract;

namespace TahaKonsept.Agent.Abstractions.Gpu.Base;

/// <summary>Sistem klasöründe sürücü DLL'lerinin varlığına bakan yoklama tabanı; alt sınıf <see cref="SystemDirectory"/> sağlar.</summary>
public abstract class GpuCapabilityProbeBase : IGpuCapabilityProbe
{
    /// <inheritdoc />
    public bool HasNvidiaDriver => File.Exists(Path.Combine(SystemDirectory, "nvcuda.dll"));

    /// <inheritdoc />
    public bool HasVulkanRuntime => File.Exists(Path.Combine(SystemDirectory, "vulkan-1.dll"));

    /// <summary>Sürücü DLL'lerinin arandığı klasör.</summary>
    protected abstract string SystemDirectory { get; }
}
