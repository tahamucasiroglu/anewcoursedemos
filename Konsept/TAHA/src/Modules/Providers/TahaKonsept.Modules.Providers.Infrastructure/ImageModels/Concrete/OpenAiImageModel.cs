using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Base;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;

/// <summary>
/// OpenAI görsel modelleri (gpt-image-2, gpt-image-1-mini): <c>POST v1/images/generations</c> (Bearer), <c>n</c> = adet,
/// <c>size</c> = en yakın sabit boyut (1024x1024 / 1024x1536 / 1536x1024), <c>quality: medium</c>, <c>output_format: png</c>;
/// yanıt <c>data[].b64_json</c>. Maliyet: katalogda token fiyatı varsa ve yanıt <c>usage</c> verirse token bazlı, yoksa görsel başı
/// (çift sayım olmaz).
/// </summary>
public sealed class OpenAiImageModel : HttpImageModelBase
{
    /// <summary>API adresi.</summary>
    public static readonly Uri Endpoint = new("https://api.openai.com/v1/images/generations");

    /// <summary>Desteklenen boyutlar.</summary>
    private static readonly IReadOnlyList<string> Sizes = ["1024x1024", "1024x1536", "1536x1024"];

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="options">Zamanlama ayarları.</param>
    public OpenAiImageModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http, MediaModelOptions options)
        : base(model, credential, dependencies, http, options)
    {
    }

    /// <inheritdoc />
    protected override async Task<Result<ImageModelOutput>> GenerateCoreAsync(ImageRequest request, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = ModelId,
            ["prompt"] = PromptWithNegative(request),
            ["n"] = Math.Clamp(request.Count, 1, 10),
            ["size"] = ImageSizes.NearestSize(request.Width, request.Height, Sizes),
            ["quality"] = "medium",
            ["output_format"] = "png",
        };
        var message = MediaHttp.Json(HttpMethod.Post, Endpoint, body);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Credential.ApiKey);
        var response = await SendAsync(message, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        return ReadJson(response.Value, root =>
        {
            var images = MediaJson.Items(root, "data")
                .Select(d => MediaJson.Text(d, "b64_json"))
                .Where(b => !string.IsNullOrEmpty(b))
                .Select(b => Convert.FromBase64String(b!))
                .ToList();
            if (images.Count == 0)
            {
                return Error.Failure(ProviderErrorCodes.EmptyResponse, $"{PlatformDisplayName} görsel döndürmedi.");
            }

            var usage = MediaJson.Path(root, "usage") ?? default;
            var tokens = new CallUsage
            {
                InputTokens = MediaJson.Number(usage, "input_tokens"),
                OutputTokens = MediaJson.Number(usage, "output_tokens"),
            };
            var tokenPriced = Model.Pricing.InputPerMTok is not null || Model.Pricing.OutputPerMTok is not null;
            var billed = tokenPriced && tokens.OutputTokens > 0 ? tokens : new CallUsage { Images = images.Count };
            return Result.Success(new ImageModelOutput(images, "image/png", billed));
        });
    }
}
