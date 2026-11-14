using TahaKonsept.Agent.Abstractions.Gpu.Abstract;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Support;

/// <summary>GPU'suz test için sabit yanıtlı GPU yoklaması (NVIDIA sürücüsü var/yok).</summary>
public sealed class FakeGpuCapabilityProbe : IGpuCapabilityProbe
{
    /// <summary>Yanıtla kurar.</summary>
    /// <param name="hasNvidiaDriver">NVIDIA sürücüsü var mı.</param>
    public FakeGpuCapabilityProbe(bool hasNvidiaDriver)
    {
        HasNvidiaDriver = hasNvidiaDriver;
    }

    /// <inheritdoc />
    public bool HasNvidiaDriver { get; }

    /// <inheritdoc />
    public bool HasVulkanRuntime => false;
}
