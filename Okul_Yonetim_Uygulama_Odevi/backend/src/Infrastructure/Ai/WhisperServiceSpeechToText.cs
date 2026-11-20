using System.Net.Http.Headers;
using System.Text.Json;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Infrastructure.Ai;

/// <summary>
/// Yerel ses servisi adaptörü (spec 0012 C-1, AC-9; <c>tools/ai/stt_server.py</c>): sesi ham gövde olarak
/// <c>POST transcribe</c> adresine gönderir (içerik türü başlıkta), yanıttan dökümü (<c>text</c>) ve süreyi (<c>duration</c>,
/// sn) okur. Ses bellekten akar, diske yazılmaz. Servis hatası, zaman aşımı ya da eksik alanlı yanıt
/// <see cref="AiUnavailableException"/>. Çağrı <see cref="AiWorkGate"/> sırasından geçer.
/// </summary>
public sealed class WhisperServiceSpeechToText : ISpeechToText
{
    private readonly HttpClient http;
    private readonly AiWorkGate gate;

    public WhisperServiceSpeechToText(HttpClient http, AiWorkGate gate)
    {
        this.http = http;
        this.gate = gate;
    }

    public Task<SpeechToTextResult> TranscribeAsync(Stream audio, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        return this.gate.RunAsync(token => this.CallAsync(audio, contentType, token), cancellationToken);
    }

    private async Task<SpeechToTextResult> CallAsync(Stream audio, string contentType, CancellationToken cancellationToken)
    {
        using StreamContent content = new(audio);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        JsonElement json = await AiHttp.PostAsync(this.http, "transcribe", content, cancellationToken).ConfigureAwait(false);
        if (json.ValueKind != JsonValueKind.Object
            || !json.TryGetProperty("text", out JsonElement text) || text.ValueKind != JsonValueKind.String
            || !json.TryGetProperty("duration", out JsonElement duration) || duration.ValueKind != JsonValueKind.Number)
        {
            throw new AiUnavailableException("invalid-response");
        }

        return new SpeechToTextResult(text.GetString()!.Trim(), duration.GetDouble());
    }
}
