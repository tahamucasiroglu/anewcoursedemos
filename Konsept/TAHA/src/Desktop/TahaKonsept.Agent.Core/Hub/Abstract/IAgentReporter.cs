using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Agent.Core.Hub.Abstract;

/// <summary>
/// Ajandan sunucuya rapor kanalı (hub'ın <c>IAgentHub</c> metotları). Bağlantı yokken ilerleme raporları atılır;
/// tamamlandı/başarısız raporları kuyruğa alınıp yeniden bağlanınca gönderilir.
/// </summary>
public interface IAgentReporter
{
    /// <summary>Aşama ilerlemesi.</summary>
    /// <param name="report">Rapor.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task ReportProgressAsync(StageProgressReport report, CancellationToken ct);

    /// <summary>Aşama tamamlandı.</summary>
    /// <param name="report">Rapor.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task ReportCompletedAsync(StageResultReport report, CancellationToken ct);

    /// <summary>Aşama başarısız ya da durduruldu.</summary>
    /// <param name="report">Rapor.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task ReportFailedAsync(StageFailureReport report, CancellationToken ct);

    /// <summary>Günlük kaydı gönderir (bağlantı yoksa atılır).</summary>
    /// <param name="entry">Kayıt.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task LogAsync(AgentLogEntry entry, CancellationToken ct);

    /// <summary>Ses önizleme sonucu.</summary>
    /// <param name="result">Sonuç.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    Task ReportSpeechPreviewAsync(SpeechPreviewResult result, CancellationToken ct);
}
