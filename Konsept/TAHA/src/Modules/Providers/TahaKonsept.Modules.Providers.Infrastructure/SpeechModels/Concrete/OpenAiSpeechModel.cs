using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Concrete;

/// <summary>
/// OpenAI gpt-4o-mini-tts: <c>POST v1/audio/speech</c> (Bearer), <c>{model, input, voice, instructions, response_format, speed}</c>;
/// yanıt doğrudan ses baytlarıdır. <c>instructions</c> Türkçe doğal anlatıcı tonu ister. Maliyet: süre ölçülür → dakika fiyatı.
/// </summary>
public sealed class OpenAiSpeechModel : HttpSpeechModelBase
{
    /// <summary>API adresi.</summary>
    public static readonly Uri Endpoint = new("https://api.openai.com/v1/audio/speech");

    /// <summary>Okuma talimatı (Türkçe anlatım).</summary>
    public const string TurkishInstructions =
        "Türkçe metni doğal, akıcı ve anlaşılır bir belgesel anlatıcısı tonuyla oku. Türkçe vurgu ve tonlamayı doğru kullan; acele etme.";

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    public OpenAiSpeechModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
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

        var format = FormatOf(request);
        var body = new JsonObject
        {
            ["model"] = ModelId,
            ["input"] = request.Text,
            ["voice"] = voice.Value,
            ["response_format"] = format,
            ["speed"] = Math.Clamp(request.Speed, 0.25m, 4m),
        };
        if (request.Language.StartsWith("tr", StringComparison.OrdinalIgnoreCase))
        {
            body["instructions"] = TurkishInstructions;
        }

        var message = MediaHttp.Json(HttpMethod.Post, Endpoint, body);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Credential.ApiKey);
        var response = await SendAsync(message, ct);
        return response.IsFailure ? response.Error : Output(response.Value.Body, AudioFormats.MimeOf(format), request);
    }
}
