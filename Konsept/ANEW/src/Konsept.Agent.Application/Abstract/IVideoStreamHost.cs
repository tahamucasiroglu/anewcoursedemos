using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Ajanın yerel video akışı (spec 0017 Req 21, Ç-1): yalnızca bu bilgisayardan (127.0.0.1) açılır; her video için kısa ömürlü, tek videoya
/// bağlı yetki üretir. Yetki ilk açılıştan sonra oynatma sürdükçe geçerli kalır (uzun video kesilmez). Yetki loglanmaz.
/// </summary>
public interface IVideoStreamHost : IAsyncDisposable
{
    /// <summary>Gets dinlenen adresin portu (başlamadıysa 0).</summary>
    int Port { get; }

    /// <summary>Dinlemeyi başlatır.</summary>
    void Start();

    /// <summary>Video için akış adresi açar.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Adres ya da "dosya yok".</returns>
    AgentStreamReply Open(AgentStreamRequest request);
}
