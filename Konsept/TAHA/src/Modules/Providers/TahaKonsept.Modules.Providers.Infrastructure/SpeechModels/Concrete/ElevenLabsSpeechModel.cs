using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Concrete;

/// <summary>
/// ElevenLabs (v4, v4 Turbo): <c>POST v1/text-to-speech/{voice_id}?output_format=…</c> (<c>xi-api-key</c>),
/// <c>{text, model_id, language_code, voice_settings.speed}</c>. mp3 için <c>mp3_44100_128</c>; wav için <c>pcm_44100</c> alınıp
/// WAV'a sarılır. Maliyet bin karakter başı.
/// </summary>
public sealed class ElevenLabsSpeechModel : HttpSpeechModelBase
{
    /// <summary>API kökü.</summary>
    public static readonly Uri BaseUri = new("https://api.elevenlabs.io/v1/");

    /// <summary>PCM çıktının örnekleme hızı.</summary>
    private const int PcmSampleRate = 44100;

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public ElevenLabsSpeechModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies, http)
    {
    }

    /// <inheritdoc />
    protected override async Task<Result<SpeechModelOutput>> SynthesizeCoreAsync(SpeechRequest request, CancellationToken ct)
    {
        var voice = RequireVoice(request);
        if (voice.IsFailure)
        {
            return voice.Error;
        }

        var wav = FormatOf(request) == AudioFormats.Wav;
        var outputFormat = wav ? "pcm_" + PcmSampleRate : "mp3_44100_128";
        var body = new JsonObject
        {
            ["text"] = request.Text,
            ["model_id"] = ModelId,
            ["language_code"] = request.Language.Split('-')[0].ToLowerInvariant(),
            ["voice_settings"] = new JsonObject { ["speed"] = Math.Clamp(request.Speed, 0.7m, 1.2m) },
        };
        var uri = new Uri(BaseUri, $"text-to-speech/{Uri.EscapeDataString(voice.Value)}?output_format={outputFormat}");
        var message = MediaHttp.Json(HttpMethod.Post, uri, body);
        message.Headers.TryAddWithoutValidation("xi-api-key", Credential.ApiKey);
        var response = await SendAsync(message, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        return wav
            ? Output(AudioFormats.WrapPcm16AsWav(response.Value.Body, PcmSampleRate), "audio/wav", request)
            : Output(response.Value.Body, "audio/mpeg", request);
    }
}
