using TahaKonsept.Agent.Engines.Image.Gpu.Base;

namespace TahaKonsept.Agent.Engines.Image.Gpu.Concrete;

/// <summary>Windows yoklaması: <c>%SystemRoot%\System32</c> içinde <c>nvcuda.dll</c> / <c>vulkan-1.dll</c> arar.</summary>
public sealed class WindowsGpuCapabilityProbe : GpuCapabilityProbeBase
{
    /// <inheritdoc />
    protected override string SystemDirectory => Environment.SystemDirectory;
}
