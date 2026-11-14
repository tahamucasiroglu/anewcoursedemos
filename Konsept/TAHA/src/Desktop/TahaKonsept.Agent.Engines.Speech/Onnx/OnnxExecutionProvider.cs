namespace TahaKonsept.Agent.Engines.Speech.Onnx;

/// <summary>
/// ONNX Runtime çalıştırma sağlayıcısı: <see cref="DirectMl"/> (her DX12 GPU; NVIDIA/AMD/Intel, CUDA/cuDNN gerekmez) ve
/// <see cref="Cpu"/> (yalnız yedek: GPU yok / sürücü hatası).
/// </summary>
public enum OnnxExecutionProvider
{
    /// <summary>DirectML (GPU).</summary>
    DirectMl = 0,

    /// <summary>CPU (yedek).</summary>
    Cpu = 1,
}
