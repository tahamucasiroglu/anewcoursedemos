using System.Text.Json.Serialization;

namespace TahaKonsept.Agent.Engines.Speech.Python.Protocol;

/// <summary>
/// <c>tts_worker.py</c>'nin her komuta döndürdüğü tek satırlık JSON yanıt: <see cref="Ok"/>, hata mesajı, üretilen dosya ve süresi,
/// örnekleme hızı, ping için <see cref="Pong"/>; <c>load</c> yanıtında adapter'ın gerçekten kullandığı cihaz (<see cref="Device"/>) ve
/// CPU'ya düştüyse nedeni (<see cref="DeviceNote"/>).
/// </summary>
public sealed record WorkerResponse
{
    /// <summary>Komut başarılı mı.</summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    /// <summary>Hata mesajı (ok=false).</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>Üretilen dosya (synth).</summary>
    [JsonPropertyName("out")]
    public string? Out { get; init; }

    /// <summary>Süre (sn, synth).</summary>
    [JsonPropertyName("duration")]
    public double? Duration { get; init; }

    /// <summary>Örnekleme hızı (load/synth).</summary>
    [JsonPropertyName("sample_rate")]
    public int? SampleRate { get; init; }

    /// <summary>Ping yanıtı.</summary>
    [JsonPropertyName("pong")]
    public bool? Pong { get; init; }

    /// <summary>Adapter'ın gerçekten kullandığı cihaz (load; ör. <c>cuda</c>, <c>cpu</c>).</summary>
    [JsonPropertyName("device")]
    public string? Device { get; init; }

    /// <summary>İstenen cihaz kullanılamadıysa nedeni (load; ör. "torch CUDA'sız derlenmiş").</summary>
    [JsonPropertyName("device_note")]
    public string? DeviceNote { get; init; }
}
