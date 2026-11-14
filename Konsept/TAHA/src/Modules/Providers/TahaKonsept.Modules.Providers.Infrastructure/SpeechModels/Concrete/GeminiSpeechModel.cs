using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Concrete;

/// <summary>
/// Gemini Flash TTS: <c>generateContent</c>, <c>responseModalities: ["AUDIO"]</c>,
/// <c>speechConfig.voiceConfig.prebuiltVoiceConfig.voiceName</c>. Yanıt ham PCM (<c>audio/L16;codec=pcm;rate=24000</c>, 16 bit mono)
/// base64'tür; WAV'a sarılır (istenen biçimden bağımsız olarak çıktı her zaman WAV). Hız isteği metne okuma talimatı olarak eklenir.
/// Maliyet <c>usageMetadata</c> token'larından.
/// </summary>
public sealed class GeminiSpeechModel : HttpSpeechModelBase
{
    /// <summary>MIME'de hız yoksa varsayılan örnekleme hızı.</summary>
    private const int DefaultSampleRate = 24000;

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public GeminiSpeechModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies, http)
    {
    }

    /// <inheritdoc />
    protected override async Task<Result<SpeechModelOutput>> SynthesizeCoreAsync(SpeechRequest request, CancellationToken ct)
    {
        var config = new JsonObject { ["responseModalities"] = new JsonArray("AUDIO") };
        if (!string.IsNullOrWhiteSpace(request.Voice))
        {
            config["speechConfig"] = new JsonObject
            {
                ["voiceConfig"] = new JsonObject
                {
                    ["prebuiltVoiceConfig"] = new JsonObject { ["voiceName"] = request.Voice.Trim() },
                },
            };
        }

        var response = await SendAsync(GeminiProtocol.CreateRequest(ModelId, Credential.ApiKey, BuildText(request), config), ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        return ReadJson(response.Value, root =>
        {
            var parsed = GeminiProtocol.Parse(root, PlatformDisplayName);
            if (parsed.IsFailure)
            {
                return parsed.Error;
            }

            var audio = parsed.Value.Parts.Where(p => p.MimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)).ToList();
            if (audio.Count == 0)
            {
                return Error.Failure(ProviderErrorCodes.EmptyResponse, $"{PlatformDisplayName} ses döndürmedi.");
            }

            var pcm = audio.SelectMany(p => p.Data).ToArray();
            var rate = AudioFormats.SampleRateFromMime(audio[0].MimeType, DefaultSampleRate);
            return Output(AudioFormats.WrapPcm16AsWav(pcm, rate), "audio/wav", request, parsed.Value.TokenUsage);
        });
    }

    /// <summary>Seslendirilecek metni kurar (hız normal değilse kısa okuma talimatı eklenir).</summary>
    /// <param name="request">İstek.</param>
    private static string BuildText(SpeechRequest request) => request.Speed switch
    {
        < 0.95m => "Yavaş ve sakin oku: " + request.Text,
        > 1.05m => "Biraz hızlı oku: " + request.Text,
        _ => request.Text,
    };
}
