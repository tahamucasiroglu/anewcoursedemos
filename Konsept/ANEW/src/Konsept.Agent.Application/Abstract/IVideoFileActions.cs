using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Üretilen videonun dosya eylemleri (spec 0007 Req 4): videonun klasörünü dosya gezgininde (dosya seçili) ya da videoyu bilgisayarın
/// varsayılan video uygulamasında açar. Yalnızca video klasöründeki dosya adı kabul edilir; dosya yoksa "bulunamadı" (Req 9).
/// </summary>
public interface IVideoFileActions
{
    /// <summary>Eylemi uygular.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Sonuç.</returns>
    AgentFileActionReply Open(AgentFileActionRequest request);
}
