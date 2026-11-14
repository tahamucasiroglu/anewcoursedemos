using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// "Ajanı test et": API → ajan komut yolunun kanıtı (spec 0001 Req 8, AC-7).
/// </summary>
public interface IAgentTestService
{
    /// <summary>Ajana komut gönderir; 10 sn içinde cevap gelmezse zaman aşımı döner.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Ajanın bilgileri ya da hata (ajan bağlı değil / zaman aşımı).</returns>
    Task<Result<AgentTestResultDto>> TestAsync(CancellationToken cancellationToken);
}
