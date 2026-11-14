namespace TahaKonsept.Agent.Engines.Speech.Devices;

/// <summary>
/// Python ses motorunun hangi cihazda çalışacağı kararı ("Önce GPU" kuralı): <see cref="Device"/> (<c>cuda</c> ya da <c>cpu</c>) ve
/// GPU kullanabilen bir motor CPU'ya düştüyse nedeni (<see cref="FallbackReason"/>; uyarı olarak loglanır, sessiz düşüş yok).
/// <see cref="UsesGpu"/> CUDA tekerlek dizininin kurulup kurulmayacağını da belirler.
/// </summary>
/// <param name="Device">Adapter'a giden cihaz: <c>cuda</c> ya da <c>cpu</c>.</param>
/// <param name="FallbackReason">GPU kullanabilen motor CPU'ya düştüyse nedeni; tasarım gereği CPU ya da GPU seçildiyse <c>null</c>.</param>
public sealed record TorchDeviceDecision(string Device, string? FallbackReason)
{
    /// <summary>CUDA cihaz adı.</summary>
    public const string Cuda = "cuda";

    /// <summary>CPU cihaz adı.</summary>
    public const string Cpu = "cpu";

    /// <summary>GPU (CUDA) kararı.</summary>
    public static TorchDeviceDecision Gpu { get; } = new(Cuda, null);

    /// <summary>Tasarım gereği CPU (motor GPU kullanamıyor; uyarı gerekmez).</summary>
    public static TorchDeviceDecision CpuByDesign { get; } = new(Cpu, null);

    /// <summary>Karar CUDA mı.</summary>
    public bool UsesGpu => Device.StartsWith(Cuda, StringComparison.OrdinalIgnoreCase);

    /// <summary>GPU kullanabilen motor için CPU'ya düşüş kararı.</summary>
    /// <param name="reason">Neden (loga yazılır).</param>
    /// <returns>Karar.</returns>
    public static TorchDeviceDecision CpuFallback(string reason) => new(Cpu, reason);
}
