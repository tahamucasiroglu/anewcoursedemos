using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Abstract;

/// <summary>Ajanın Error/Critical log özetlerini veritabanındaki teknik loga yazar (spec 0008 Req 7).</summary>
public interface IAgentLogService
{
    /// <summary>
    /// Özeti kabul eder: yalnızca kayıtlı (geçerli) ajan bağlantısından, yalnızca Error/Critical, dakikalık kota
    /// içinde; mesaj en fazla <c>4000</c> karakter, kaynak "ajan".
    /// </summary>
    /// <param name="connectionId">Gönderen bağlantı.</param>
    /// <param name="summary">Özet.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Yazıldıysa <c>true</c>.</returns>
    Task<bool> ReportAsync(string connectionId, AgentLogSummary summary, CancellationToken cancellationToken);
}
