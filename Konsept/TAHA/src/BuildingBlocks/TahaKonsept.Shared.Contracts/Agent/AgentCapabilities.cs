namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanın donanım ve kurulu bileşen özeti: GPU'lar, RAM/disk, kurulu motorlar ve modeller.
/// </summary>
public sealed record AgentCapabilities
{
    /// <summary>Algılanan GPU'lar.</summary>
    public IReadOnlyList<GpuInfo> Gpus { get; init; } = [];

    /// <summary>Kurulu bileşenler.</summary>
    public IReadOnlyList<InstalledComponent> Components { get; init; } = [];

    /// <summary>Toplam RAM (MB).</summary>
    public long TotalRamMb { get; init; }

    /// <summary>Çalışma alanı diskindeki boş alan (MB).</summary>
    public long FreeDiskMb { get; init; }
}
