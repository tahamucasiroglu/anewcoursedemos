using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Capabilities.Abstract;

/// <summary>
/// Donanım ve kurulu bileşenleri algılar: GPU'lar (<c>nvidia-smi</c>), RAM, çalışma alanı diskindeki boş alan,
/// tüm motorların (<c>IsInstalled</c>) ve ön gereksinimlerin durumu. Sonuç <c>Hello</c> içinde sunucuya gider.
/// </summary>
public interface ICapabilityDetector
{
    /// <summary>Algılamayı yapar.</summary>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yetenekler.</returns>
    Task<AgentCapabilities> DetectAsync(CancellationToken ct);
}
