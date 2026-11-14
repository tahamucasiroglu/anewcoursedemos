using TahaKonsept.Agent.Abstractions.Gpu.Base;

namespace TahaKonsept.Agent.Abstractions.Gpu.Concrete;

/// <summary>Windows yoklaması: <c>%SystemRoot%\System32</c> içinde <c>nvcuda.dll</c> / <c>vulkan-1.dll</c> arar.</summary>
public sealed class WindowsGpuCapabilityProbe : GpuCapabilityProbeBase
{
    /// <inheritdoc />
    protected override string SystemDirectory => Environment.SystemDirectory;
}
