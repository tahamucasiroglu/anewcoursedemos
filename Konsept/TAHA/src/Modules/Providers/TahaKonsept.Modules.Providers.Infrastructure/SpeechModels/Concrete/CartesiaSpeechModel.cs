using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Concrete;

/// <summary>
/// Cartesia Sonic: <c>POST https://api.cartesia.ai/tts/bytes</c> (<c>X-API-Key</c>, <c>Cartesia-Version</c>),
/// <c>{model_id, transcript, voice {mode: id, id}, language, output_format}</c>; mp3 (44.1 kHz, 128 kbps) ya da wav (pcm_s16le).
/// Yanıt doğrudan ses baytlarıdır. Maliyet katalogda tanımsız (0; fiyat doğrulanınca <c>perKChars</c> eklenir).
/// </summary>
public sealed class CartesiaSpeechModel : HttpSpeechModelBase
{
    /// <summary>API adresi.</summary>
    public static readonly Uri Endpoint = new("https://api.cartesia.ai/tts/bytes");

    /// <summary>API sürüm başlığı (anahtar testiyle aynı).</summary>
    public const string ApiVersion = "2025-04-16";

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public CartesiaSpeechModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
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
        var outputFormat = wav
            ? new JsonObject { ["container"] = "wav", ["encoding"] = "pcm_s16le", ["sample_rate"] = 44100 }
            : new JsonObject { ["container"] = "mp3", ["sample_rate"] = 44100, ["bit_rate"] = 128000 };
        var body = new JsonObject
        {
            ["model_id"] = ModelId,
            ["transcript"] = request.Text,
            ["voice"] = new JsonObject { ["mode"] = "id", ["id"] = voice.Value },
            ["language"] = request.Language.Split('-')[0].ToLowerInvariant(),
            ["output_format"] = outputFormat,
        };
        var message = MediaHttp.Json(HttpMethod.Post, Endpoint, body);
        message.Headers.TryAddWithoutValidation("X-API-Key", Credential.ApiKey);
        message.Headers.TryAddWithoutValidation("Cartesia-Version", ApiVersion);
        var response = await SendAsync(message, ct);
        return response.IsFailure ? response.Error : Output(response.Value.Body, wav ? "audio/wav" : "audio/mpeg", request);
    }
}
