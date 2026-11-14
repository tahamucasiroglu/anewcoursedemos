using System.Globalization;
using System.Security;
using System.Text;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Concrete;

/// <summary>
/// Azure Neural TTS (REST): <c>POST https://{bölge}.tts.speech.microsoft.com/cognitiveservices/v1</c>
/// (<c>Ocp-Apim-Subscription-Key</c>, <c>X-Microsoft-OutputFormat</c>), gövde SSML (<see cref="BuildSsml"/>: metin XML-kaçışlı,
/// <c>prosody rate</c> hız çarpanından; ses istek dilinde değilse — çok dilli sesler — metin <c>&lt;lang xml:lang&gt;</c> ile sarılır).
/// Bölge anahtar kasasından (<see cref="ProviderCredential.Region"/>). Maliyet bin karakter başı.
/// </summary>
public sealed class AzureSpeechModel : HttpSpeechModelBase
{
    /// <summary>MP3 çıktı biçimi.</summary>
    public const string Mp3OutputFormat = "audio-24khz-96kbitrate-mono-mp3";

    /// <summary>WAV çıktı biçimi.</summary>
    public const string WavOutputFormat = "riff-24khz-16bit-mono-pcm";

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi (bölge dolu olmalı).</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public AzureSpeechModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies, http)
    {
    }

    /// <summary>SSML belgesini kurar.</summary>
    /// <param name="text">Metin.</param>
    /// <param name="voice">Ses adı (ör. <c>tr-TR-AhmetNeural</c>).</param>
    /// <param name="language">İstek dili (BCP-47).</param>
    /// <param name="speed">Hız çarpanı.</param>
    public static string BuildSsml(string text, string voice, string language, decimal speed)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(voice);
        ArgumentNullException.ThrowIfNull(language);
        var voiceLanguage = voice.Length >= 5 ? voice[..5] : language;
        var escaped = SecurityElement.Escape(text);
        var spoken = string.Equals(voiceLanguage, language, StringComparison.OrdinalIgnoreCase)
            ? escaped
            : $"<lang xml:lang=\"{SecurityElement.Escape(language)}\">{escaped}</lang>";
        var rate = ((Math.Clamp(speed, 0.5m, 2m) - 1m) * 100m).ToString("+0;-0;+0", CultureInfo.InvariantCulture) + "%";
        return new StringBuilder()
            .Append("<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"")
            .Append(SecurityElement.Escape(voiceLanguage))
            .Append("\"><voice name=\"")
            .Append(SecurityElement.Escape(voice))
            .Append("\"><prosody rate=\"")
            .Append(rate)
            .Append("\">")
            .Append(spoken)
            .Append("</prosody></voice></speak>")
            .ToString();
    }

    /// <inheritdoc />
    protected override async Task<Result<SpeechModelOutput>> SynthesizeCoreAsync(SpeechRequest request, CancellationToken ct)
    {
        var voice = RequireVoice(request);
        if (voice.IsFailure)
        {
            return voice.Error;
        }

        if (string.IsNullOrWhiteSpace(Credential.Region))
        {
            return Error.Validation(ProviderErrorCodes.KeyMissing, "Azure Speech için bölge girilmemiş (Ayarlar → API anahtarları, ör. westeurope).");
        }

        var wav = FormatOf(request) == AudioFormats.Wav;
        var uri = new Uri($"https://{Credential.Region.Trim().ToLowerInvariant()}.tts.speech.microsoft.com/cognitiveservices/v1");
        var message = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new StringContent(BuildSsml(request.Text, voice.Value, request.Language, request.Speed), Encoding.UTF8, "application/ssml+xml"),
        };
        message.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", Credential.ApiKey);
        message.Headers.TryAddWithoutValidation("X-Microsoft-OutputFormat", wav ? WavOutputFormat : Mp3OutputFormat);
        message.Headers.TryAddWithoutValidation("User-Agent", "TahaKonsept");
        var response = await SendAsync(message, ct);
        return response.IsFailure ? response.Error : Output(response.Value.Body, wav ? "audio/wav" : "audio/mpeg", request);
    }
}
