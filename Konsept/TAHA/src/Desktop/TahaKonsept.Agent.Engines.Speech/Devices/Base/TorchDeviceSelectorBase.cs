using TahaKonsept.Agent.Engines.Speech.Devices.Abstract;

namespace TahaKonsept.Agent.Engines.Speech.Devices.Base;

/// <summary>
/// <see cref="ITorchDeviceSelector"/> karar mantığı: GPU kullanamayan motor → tasarım gereği CPU; GPU kullanabilen motor →
/// <see cref="GpuUnavailableReason"/> <c>null</c> ise CUDA, değilse nedenle birlikte CPU (çağıran uyarı loglar).
/// Somut sınıf yalnızca GPU'nun neden kullanılamadığını söyler (ör. NVIDIA sürücüsü yok).
/// </summary>
public abstract class TorchDeviceSelectorBase : ITorchDeviceSelector
{
    /// <inheritdoc />
    public TorchDeviceDecision Select(bool supportsGpu)
    {
        if (!supportsGpu)
        {
            return TorchDeviceDecision.CpuByDesign;
        }

        var reason = GpuUnavailableReason();
        return reason is null ? TorchDeviceDecision.Gpu : TorchDeviceDecision.CpuFallback(reason);
    }

    /// <summary>CUDA kullanılamıyorsa nedeni; kullanılabiliyorsa <c>null</c>.</summary>
    /// <returns>Neden ya da <c>null</c>.</returns>
    protected abstract string? GpuUnavailableReason();
}
