namespace TahaKonsept.Shared.Contracts.Agent.Payloads;

/// <summary>
/// <c>Speech</c> aşamasının ajan girdisi: yerel ses motoru, dil, ses ve sahne sahne okunuş metinleri.
/// Ajan her sahneyi <c>speech/scene-&lt;i&gt;.wav</c> olarak üretir, sonra tek <c>speech/speech.wav</c> dosyasına birleştirir;
/// sonuç JSON'u <see cref="SpeechStageResult"/>'tır.
/// </summary>
public sealed record SpeechStagePayload
{
    /// <summary>Ses motoru anahtarı (ör. <c>supertonic-3</c>).</summary>
    public string EngineKey { get; init; } = string.Empty;

    /// <summary>Dil kodu (ör. <c>tr</c>).</summary>
    public string Language { get; init; } = "tr";

    /// <summary>Hazır ses kimliği (isteğe bağlı).</summary>
    public string? VoiceId { get; init; }

    /// <summary>Klonlama için referans ses artefaktının adı (<c>StageCommand.Artifacts</c> içinden; isteğe bağlı).</summary>
    public string? ReferenceAudioArtifactName { get; init; }

    /// <summary>Hız çarpanı (1.0 normal).</summary>
    public double Speed { get; init; } = 1.0;

    /// <summary>Sahne sahne metinler (sıralı).</summary>
    public IReadOnlyList<SpeechSegmentItem> Segments { get; init; } = [];
}
