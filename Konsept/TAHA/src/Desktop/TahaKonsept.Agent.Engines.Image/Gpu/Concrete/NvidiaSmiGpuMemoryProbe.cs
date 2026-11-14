using TahaKonsept.Agent.Abstractions.Gpu.Abstract;
using TahaKonsept.Agent.Abstractions.Processes;
using TahaKonsept.Agent.Abstractions.Processes.Abstract;
using TahaKonsept.Agent.Engines.Image.Gpu.Abstract;
using TahaKonsept.Agent.Engines.Image.Gpu.Base;

namespace TahaKonsept.Agent.Engines.Image.Gpu.Concrete;

/// <summary>
/// <c>nvidia-smi</c> ile boş VRAM okuyan yoklama (PATH'ten; NVIDIA sürücüsü <c>System32</c>'ye koyar). NVIDIA sürücüsü yoksa
/// (<see cref="IGpuCapabilityProbe.HasNvidiaDriver"/>) süreç başlatılmaz. Zaman aşımı 10 sn.
/// </summary>
public sealed class NvidiaSmiGpuMemoryProbe : GpuMemoryProbeBase
{
    private static readonly string[] QueryArgs = ["--query-gpu=memory.free", "--format=csv,noheader,nounits"];

    private readonly IProcessRunner _processRunner;
    private readonly IGpuCapabilityProbe _capabilities;

    /// <summary>Oluşturur.</summary>
    /// <param name="processRunner">Süreç çalıştırıcı.</param>
    /// <param name="capabilities">Sürücü yoklaması.</param>
    public NvidiaSmiGpuMemoryProbe(IProcessRunner processRunner, IGpuCapabilityProbe capabilities)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    /// <inheritdoc />
    protected override async Task<string?> QueryAsync(CancellationToken ct)
    {
        if (!_capabilities.HasNvidiaDriver)
        {
            return null;
        }

        var result = await _processRunner.RunAsync(new ProcessSpec("nvidia-smi", QueryArgs, Timeout: TimeSpan.FromSeconds(10)), null, ct).ConfigureAwait(false);
        return result.Succeeded ? result.StandardOutput : null;
    }
}
