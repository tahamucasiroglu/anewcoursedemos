using System.Globalization;
using TahaKonsept.Agent.Core.Capabilities.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Capabilities.Base;

/// <summary>
/// Yetenek algılayıcı tabanı: GPU, bileşen, RAM ve disk bilgisini toplayıp <see cref="AgentCapabilities"/> üretir.
/// <see cref="ParseNvidiaSmi"/> <c>nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits</c> çıktısını çözer.
/// </summary>
public abstract class CapabilityDetectorBase : ICapabilityDetector
{
    /// <summary>nvidia-smi sorgu argümanları.</summary>
    public static readonly IReadOnlyList<string> NvidiaSmiArguments =
        ["--query-gpu=name,memory.total,driver_version", "--format=csv,noheader,nounits"];

    /// <inheritdoc />
    public async Task<AgentCapabilities> DetectAsync(CancellationToken ct)
    {
        var gpus = await DetectGpusAsync(ct);
        return new AgentCapabilities
        {
            Gpus = gpus,
            Components = DetectComponents(),
            TotalRamMb = DetectTotalRamMb(),
            FreeDiskMb = DetectFreeDiskMb(),
        };
    }

    /// <summary>nvidia-smi CSV çıktısını GPU listesine çevirir. Boş/bozuk satırlar atlanır.</summary>
    /// <param name="output">Çıktı (satır başına bir GPU: <c>ad, VRAM MB, sürücü</c>).</param>
    /// <returns>GPU'lar.</returns>
    public static IReadOnlyList<GpuInfo> ParseNvidiaSmi(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var gpus = new List<GpuInfo>();
        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = rawLine.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            if (!long.TryParse(parts[1].Replace("MiB", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var vram))
            {
                continue;
            }

            gpus.Add(new GpuInfo
            {
                Name = parts[0],
                Vendor = "NVIDIA",
                VramMb = vram,
                CudaAvailable = true,
                DriverVersion = parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2]) ? parts[2] : null,
            });
        }

        return gpus;
    }

    /// <summary>GPU'ları algılar.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>GPU'lar (yoksa boş).</returns>
    protected abstract Task<IReadOnlyList<GpuInfo>> DetectGpusAsync(CancellationToken ct);

    /// <summary>Kurulu bileşenleri listeler.</summary>
    /// <returns>Bileşenler.</returns>
    protected abstract IReadOnlyList<InstalledComponent> DetectComponents();

    /// <summary>Çalışma alanı diskindeki boş alan (MB).</summary>
    /// <returns>MB.</returns>
    protected abstract long DetectFreeDiskMb();

    /// <summary>Toplam fiziksel bellek (MB; GC'nin gördüğü).</summary>
    /// <returns>MB.</returns>
    protected virtual long DetectTotalRamMb() => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);
}
