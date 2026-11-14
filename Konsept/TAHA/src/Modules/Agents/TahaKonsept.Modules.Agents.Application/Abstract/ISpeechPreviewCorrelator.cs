using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Ses önizleme isteği ↔ ajan yanıtı eşleyici (singleton). Akış: <see cref="Register"/> (komut gönderilmeden önce) →
/// ajan <c>SpeechPreviewCompleted</c> → <see cref="Complete"/>; bekleme zaman aşımı/iptalde <see cref="Cancel"/>.
/// </summary>
public interface ISpeechPreviewCorrelator
{
    /// <summary>Bekleyen istek kaydeder.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <returns>Yanıt gelince tamamlanan görev.</returns>
    Task<SpeechPreviewResult> Register(Guid requestId);

    /// <summary>Yanıtı bekleyen isteğe iletir.</summary>
    /// <param name="result">Yanıt.</param>
    /// <returns>Bekleyen istek varsa <c>true</c>.</returns>
    bool Complete(SpeechPreviewResult result);

    /// <summary>Bekleyen isteği iptal eder.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    void Cancel(Guid requestId);

    /// <summary>Bekleyen istek sayısı.</summary>
    int PendingCount { get; }
}
