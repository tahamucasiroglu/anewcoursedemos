using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Ses önizleme isteği ↔ ajan yanıtı eşleyici (singleton). Akış: <see cref="Register"/> (komut gönderilmeden önce) →
/// ajan <c>SpeechPreviewCompleted</c> → <see cref="Complete"/>; bekleme zaman aşımı/iptalde <see cref="Cancel"/>.
/// Her istek sahibinin kullanıcı kimliğiyle kaydedilir; yalnız aynı kullanıcının ajanından gelen yanıt isteği tamamlar.
/// </summary>
public interface ISpeechPreviewCorrelator
{
    /// <summary>Bekleyen istek kaydeder.</summary>
    /// <param name="userId">İsteği yapan (ajanı sahibi) kullanıcı.</param>
    /// <param name="requestId">İstek kimliği.</param>
    /// <returns>Yanıt gelince tamamlanan görev.</returns>
    Task<SpeechPreviewResult> Register(Guid userId, Guid requestId);

    /// <summary>Yanıtı bekleyen isteğe iletir (istek başka kullanıcınınsa dokunmaz).</summary>
    /// <param name="userId">Yanıtı gönderen ajanın kullanıcısı.</param>
    /// <param name="result">Yanıt.</param>
    /// <returns>Bu kullanıcıya ait bekleyen istek varsa <c>true</c>.</returns>
    bool Complete(Guid userId, SpeechPreviewResult result);

    /// <summary>Bekleyen isteği iptal eder.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    void Cancel(Guid requestId);

    /// <summary>Bekleyen istek sayısı.</summary>
    int PendingCount { get; }
}
