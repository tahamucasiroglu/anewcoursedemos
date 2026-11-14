using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using TahaKonsept.Agent.Engines.Speech.Onnx.Base;

namespace TahaKonsept.Agent.Engines.Speech.Onnx.Concrete;

/// <summary>
/// DirectML → CPU oturum fabrikası (paket <c>Microsoft.ML.OnnxRuntime.DirectML</c>; CUDA/cuDNN DLL'i gerektirmez, her DX12 GPU'da çalışır).
/// DirectML: <c>performance_preference=high_performance</c> + <c>device_filter=gpu</c> ile yüksek performanslı GPU seçilir (Optimus
/// dizüstüde tümleşik GPU değil RTX); bu seçenekler desteklenmezse varsayılan adaptör (cihaz 0) uyarıyla denenir. DirectML oturumları
/// bellek deseni kapalı ve sıralı çalıştırma ister. CPU: tam grafik iyileştirme. <see cref="GpuAdapterName"/>: DXGI'nin yüksek performans
/// sırasındaki ilk donanım adaptörü (DirectML'in <c>high_performance</c> seçimiyle aynı sıra; <see cref="DxgiAdapterQuery"/>).
/// </summary>
public sealed class DirectMlOnnxSessionFactory : OnnxSessionFactoryBase
{
    private readonly Lazy<string?> _adapterName = new(DxgiAdapterQuery.HighPerformanceAdapterName);

    private static readonly Dictionary<string, string> HighPerformanceGpu = new(StringComparer.Ordinal)
    {
        ["performance_preference"] = "high_performance",
        ["device_filter"] = "gpu",
    };

    /// <summary>Günlükçü ile kurar.</summary>
    /// <param name="logger">Günlükçü.</param>
    public DirectMlOnnxSessionFactory(ILogger<DirectMlOnnxSessionFactory> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    public override string? GpuAdapterName => _adapterName.Value;

    /// <inheritdoc />
    protected override void ConfigureProvider(SessionOptions options, OnnxExecutionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        if (provider != OnnxExecutionProvider.DirectMl)
        {
            return;
        }

        options.EnableMemoryPattern = false;
        options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
        try
        {
            options.AppendExecutionProvider("DML", HighPerformanceGpu);
        }
        catch (OnnxRuntimeException ex) when (!ex.Message.Contains("No GPUs", StringComparison.OrdinalIgnoreCase)
                                              && !ex.Message.Contains("No devices", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogWarning(ex, "DirectML yüksek performanslı GPU seçimi desteklenmedi; varsayılan adaptör (cihaz 0) deneniyor");
            options.AppendExecutionProvider_DML(0);
        }
    }
}
