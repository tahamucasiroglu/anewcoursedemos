using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajandaki yerel üretim aşamaları (spec 0017 Req 13, 14): araç denetimi, görsel/ses/render/birleştirme ve Durdur ile kesme. Ajan online
/// sağlayıcıya hiç istek atmaz; online görsel Pi'den hazır gelir (Y-7, Y-8).
/// </summary>
public interface IStageRunner
{
    /// <summary>Gereken araçlardan kurulu olmayanları bildirir.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Cevap.</returns>
    AgentToolCheckReply CheckTools(AgentToolCheckRequest request);

    /// <summary>Aşamayı çalıştırır; hatayı cevapla bildirir (istisna fırlatmaz).</summary>
    /// <param name="command">Komut.</param>
    /// <returns>Cevap.</returns>
    Task<AgentStageReply> RunAsync(AgentStageCommand command);

    /// <summary>İşin süren aşamasını keser.</summary>
    /// <param name="jobId">İş.</param>
    void Cancel(Guid jobId);
}
