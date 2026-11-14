namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajanın algıladığı ekran kartı bilgisi (VRAM etiketi için).
/// </summary>
public sealed record GpuInfo
{
    /// <summary>GPU adı.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Üretici (NVIDIA, AMD, Intel...).</summary>
    public string Vendor { get; init; } = string.Empty;

    /// <summary>Toplam VRAM (MB).</summary>
    public long VramMb { get; init; }

    /// <summary>CUDA kullanılabilir mi.</summary>
    public bool CudaAvailable { get; init; }

    /// <summary>Sürücü sürümü (biliniyorsa).</summary>
    public string? DriverVersion { get; init; }
}
