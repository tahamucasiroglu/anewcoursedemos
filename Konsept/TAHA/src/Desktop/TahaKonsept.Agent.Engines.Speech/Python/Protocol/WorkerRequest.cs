using System.Text.Json.Serialization;

namespace TahaKonsept.Agent.Engines.Speech.Python.Protocol;

/// <summary>
/// <c>tts_worker.py</c>'ye gönderilen tek satırlık JSON komut. <see cref="Cmd"/>: <c>ping</c>, <c>load</c> (engine, model, device, options),
/// <c>synth</c> (text, lang, voice, ref_audio, speed, out), <c>quit</c>. Boş alanlar yazılmaz. Fabrikalar: <see cref="WorkerProtocol"/>.
/// </summary>
public sealed record WorkerRequest
{
    /// <summary>Komut adı.</summary>
    [JsonPropertyName("cmd")]
    public required string Cmd { get; init; }

    /// <summary>Adapter modülü (load).</summary>
    [JsonPropertyName("engine")]
    public string? Engine { get; init; }

    /// <summary>Model kimliği / depo (load).</summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>Cihaz: <c>cuda</c> ya da <c>cpu</c> (load).</summary>
    [JsonPropertyName("device")]
    public string? Device { get; init; }

    /// <summary>Adapter'a özel seçenekler (load).</summary>
    [JsonPropertyName("options")]
    public IReadOnlyDictionary<string, object?>? Options { get; init; }

    /// <summary>Metin (synth).</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>Dil kodu (synth).</summary>
    [JsonPropertyName("lang")]
    public string? Lang { get; init; }

    /// <summary>Ses kimliği (synth).</summary>
    [JsonPropertyName("voice")]
    public string? Voice { get; init; }

    /// <summary>Klonlama referans sesi (synth).</summary>
    [JsonPropertyName("ref_audio")]
    public string? RefAudio { get; init; }

    /// <summary>Hız çarpanı (synth).</summary>
    [JsonPropertyName("speed")]
    public double? Speed { get; init; }

    /// <summary>Çıktı .wav yolu (synth).</summary>
    [JsonPropertyName("out")]
    public string? Out { get; init; }
}
