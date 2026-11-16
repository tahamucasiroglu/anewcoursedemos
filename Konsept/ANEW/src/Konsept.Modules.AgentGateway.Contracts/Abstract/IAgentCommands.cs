using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Contracts.Abstract;

/// <summary>
/// Üretim hattının (spec 0017) ajana komut gönderdiği tek yol: komut <b>yalnızca</b> kullanıcının o anki geçerli ajan oturumuna gider
/// (BR-1, spec 0012 Req 6, 13); ajan bağlı değilse ya da komut sürerken bağlantı koparsa <c>null</c> döner (iş Ajan bekleniyor olur).
/// </summary>
public interface IAgentCommands
{
    /// <summary>Kullanıcının geçerli ajanının cihazı; bağlı değilse <c>null</c>.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <returns>Cihaz ya da <c>null</c>.</returns>
    Guid? ConnectedDevice(Guid userId);

    /// <summary>Araç denetimi.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap ya da <c>null</c> (ajan yok/koptu).</returns>
    Task<AgentToolCheckReply?> CheckToolsAsync(Guid userId, AgentToolCheckRequest request, CancellationToken cancellationToken);

    /// <summary>Yerel aşamayı çalıştırır ve sonucu bekler.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap ya da <c>null</c> (ajan yok/koptu).</returns>
    Task<AgentStageReply?> RunStageAsync(Guid userId, AgentStageCommand command, CancellationToken cancellationToken);

    /// <summary>İşin süren yerel aşamasını keser (en iyi çaba).</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="jobId">İş.</param>
    /// <returns>Görev.</returns>
    Task CancelStageAsync(Guid userId, Guid jobId);

    /// <summary>Yerel akış adresi açar.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Cevap ya da <c>null</c> (ajan yok/koptu).</returns>
    Task<AgentStreamReply?> OpenStreamAsync(Guid userId, AgentStreamRequest request, CancellationToken cancellationToken);

    /// <summary>Dosya eylemi (klasörde aç / sistem oynatıcısında aç — spec 0007 Req 4); yalnızca sahibin ajanına.</summary>
    /// <param name="userId">Kullanıcı.</param>
    /// <param name="request">İstek.</param>
    /// <param name="cancellationToken">İptal belirteci (zaman aşımı çağıranda).</param>
    /// <returns>Cevap ya da <c>null</c> (ajan yok/koptu).</returns>
    Task<AgentFileActionReply?> OpenFileAsync(Guid userId, AgentFileActionRequest request, CancellationToken cancellationToken);
}
