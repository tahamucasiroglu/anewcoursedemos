using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>
/// Ajanın hata bildirimini Monitoring'e iletir (spec 0022 Req 4, 7, 8): sahip hub kimliğinden gelir (istemciden alınmaz); bildirimin
/// hesabı sahiple uyuşmazsa kaydedilmez.
/// </summary>
public interface IAgentErrorReportService
{
    /// <summary>Bildirimi sahibin adına kaydeder.</summary>
    /// <param name="ownerUserId">Ajanın sahibi (hub kimliği).</param>
    /// <param name="request">Bildirim.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<AgentErrorReportReply> ReportAsync(Guid ownerUserId, AgentErrorReportRequest request, CancellationToken cancellationToken);
}
