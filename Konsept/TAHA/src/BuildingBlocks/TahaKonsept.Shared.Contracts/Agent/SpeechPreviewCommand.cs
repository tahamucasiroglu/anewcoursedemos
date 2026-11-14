namespace TahaKonsept.Shared.Contracts.Agent;

/// <summary>
/// Okunuş sözlüğündeki "Dinle" önizlemesi için ajana gönderilen kısa TTS isteği (<c>IAgentClient.PreviewSpeech</c>).
/// Sonuç <c>IAgentHub.SpeechPreviewCompleted</c> ile döner.
/// </summary>
public sealed record SpeechPreviewCommand
{
    /// <summary>İstek kimliği.</summary>
    public Guid RequestId { get; init; }

    /// <summary>Ses motoru kimliği (ör. <c>supertonic-3</c>).</summary>
    public string EngineId { get; init; } = string.Empty;

    /// <summary>Model kimliği (opsiyonel).</summary>
    public string? ModelId { get; init; }

    /// <summary>Ses/konuşmacı kimliği (opsiyonel).</summary>
    public string? VoiceId { get; init; }

    /// <summary>Seslendirilecek metin (okunuş).</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>Motor ayarları.</summary>
    public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();
}
