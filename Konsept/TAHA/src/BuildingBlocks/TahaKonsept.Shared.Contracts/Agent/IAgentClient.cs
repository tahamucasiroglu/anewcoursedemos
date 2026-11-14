namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Sunucu → ajan SignalR çağrıları (hub'da <c>Hub&lt;IAgentClient&gt;</c>, ajanda <c>connection.On</c> adları bu metotlarla aynıdır).
/// Önemli metotlar: <see cref="ExecuteStage"/>, <see cref="CancelJob"/>, <see cref="PauseJob"/>, <see cref="PreviewSpeech"/>.
/// </summary>
public interface IAgentClient
{
    /// <summary>Bir aşamayı çalıştır.</summary>
    /// <param name="command">Aşama komutu.</param>
    /// <returns>Görev.</returns>
    Task ExecuteStage(StageCommand command);

    /// <summary>İşi iptal et.</summary>
    /// <param name="jobId">İş kimliği.</param>
    /// <returns>Görev.</returns>
    Task CancelJob(Guid jobId);

    /// <summary>İşi duraklat (mevcut aşama/sahne bitince durur).</summary>
    /// <param name="jobId">İş kimliği.</param>
    /// <returns>Görev.</returns>
    Task PauseJob(Guid jobId);

    /// <summary>Videonun klasörünü Windows Gezgini'nde aç.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Görev.</returns>
    Task OpenFolder(Guid videoId);

    /// <summary>Videoyu sistem oynatıcısında aç.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Görev.</returns>
    Task OpenInSystemPlayer(Guid videoId);

    /// <summary>Videonun yerel dosyalarını sil.</summary>
    /// <param name="videoId">Video kimliği.</param>
    /// <returns>Görev.</returns>
    Task DeleteVideo(Guid videoId);

    /// <summary>Kısa ses önizlemesi üret.</summary>
    /// <param name="command">Önizleme komutu.</param>
    /// <returns>Görev.</returns>
    Task PreviewSpeech(SpeechPreviewCommand command);

    /// <summary>Donanım/kurulu bileşen bilgisini yeniden gönder (<c>Hello</c>).</summary>
    /// <returns>Görev.</returns>
    Task RefreshCapabilities();
}
