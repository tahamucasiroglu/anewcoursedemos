using TahaKonsept.Agent.Abstractions.Gpu.Abstract;
using TahaKonsept.Agent.Engines.Speech.Devices.Base;

namespace TahaKonsept.Agent.Engines.Speech.Devices.Concrete;

/// <summary>
/// NVIDIA sürücüsüne (<see cref="IGpuCapabilityProbe.HasNvidiaDriver"/>, <c>nvcuda.dll</c>) bakan cihaz seçici: sürücü varsa CUDA.
/// VRAM yetersizliği ve torch'un GPU'yu görmemesi Python tarafında (<c>adapters/_common.py</c>) yakalanır ve yine uyarıyla CPU'ya düşer.
/// </summary>
public sealed class NvidiaTorchDeviceSelector : TorchDeviceSelectorBase
{
    private readonly IGpuCapabilityProbe _gpu;

    /// <summary>Yoklama ile kurar.</summary>
    /// <param name="gpu">GPU yeteneği yoklaması.</param>
    public NvidiaTorchDeviceSelector(IGpuCapabilityProbe gpu)
    {
        _gpu = gpu;
    }

    /// <inheritdoc />
    protected override string? GpuUnavailableReason() =>
        _gpu.HasNvidiaDriver ? null : "NVIDIA sürücüsü (nvcuda.dll) bulunamadı";
}
