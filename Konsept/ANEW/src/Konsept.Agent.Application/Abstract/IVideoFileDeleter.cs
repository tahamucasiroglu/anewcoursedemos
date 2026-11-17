using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Agent.Application.Abstract;

/// <summary>
/// Silinen videonun dosyalarını kalıcı siler (spec 0019 Req 4): yalnızca video klasöründeki düz <c>.mp4</c> adları; diskte olmayan dosya
/// silinmiş sayılır; silinemeyen (açık dosya, izin yok) adlar cevapta döner.
/// </summary>
public interface IVideoFileDeleter
{
    /// <summary>Dosyaları siler.</summary>
    /// <param name="request">İstek.</param>
    /// <returns>Silinemeyenler.</returns>
    AgentDeleteFilesReply Delete(AgentDeleteFilesRequest request);
}
