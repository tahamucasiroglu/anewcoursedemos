namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Ajan → sunucu SignalR hub metotları (<c>/hubs/agent</c>). Hub sınıfı bu arayüzü uygular; ajan <c>InvokeAsync(nameof(...))</c> ile çağırır.
/// Önemli metotlar: <see cref="Hello"/>, <see cref="StageProgress"/>, <see cref="StageCompleted"/>, <see cref="StageFailed"/>, <see cref="Log"/>, <see cref="Heartbeat"/>.
/// </summary>
public interface IAgentHub
{
    /// <summary>Bağlantı sonrası tanıtım.</summary>
    /// <param name="hello">Tanıtım bilgisi.</param>
    /// <returns>Görev.</returns>
    Task Hello(AgentHello hello);

    /// <summary>Aşama ilerlemesi.</summary>
    /// <param name="report">Rapor.</param>
    /// <returns>Görev.</returns>
    Task StageProgress(StageProgressReport report);

    /// <summary>Aşama tamamlandı.</summary>
    /// <param name="report">Rapor.</param>
    /// <returns>Görev.</returns>
    Task StageCompleted(StageResultReport report);

    /// <summary>Aşama başarısız.</summary>
    /// <param name="report">Rapor.</param>
    /// <returns>Görev.</returns>
    Task StageFailed(StageFailureReport report);

    /// <summary>Günlük kaydı.</summary>
    /// <param name="entry">Kayıt.</param>
    /// <returns>Görev.</returns>
    Task Log(AgentLogEntry entry);

    /// <summary>Ses önizleme sonucu.</summary>
    /// <param name="result">Sonuç.</param>
    /// <returns>Görev.</returns>
    Task SpeechPreviewCompleted(SpeechPreviewResult result);

    /// <summary>Canlılık sinyali.</summary>
    /// <returns>Görev.</returns>
    Task Heartbeat();
}
