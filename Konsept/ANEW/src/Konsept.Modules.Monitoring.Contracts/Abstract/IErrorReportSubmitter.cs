using Konsept.Modules.Monitoring.Contracts.Dtos;

namespace Konsept.Modules.Monitoring.Contracts.Abstract;

/// <summary>
/// Diğer modüllerin (AgentGateway) hata bildirimi kaydettiği yol (spec 0022 Req 4, 14, 15): kaynak "ajan", "Yeni" durumu, saatlik sınır
/// (web + ajan toplamı) ve Günlük kaydı Monitoring'de uygulanır.
/// </summary>
public interface IErrorReportSubmitter
{
    /// <summary>Ajan bildirimini kullanıcı adına kaydeder.</summary>
    /// <param name="userId">Ajanın sahibi (hub kimliğinden; istemciden alınmaz).</param>
    /// <param name="entry">Bildirim.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    Task<ErrorReportSubmission> SubmitFromAgentAsync(Guid userId, AgentErrorReportEntry entry, CancellationToken cancellationToken);
}
