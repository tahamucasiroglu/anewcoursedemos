using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Base;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;

/// <summary>
/// Google Gemini görsel modelleri (Nano Banana 2.1, Gemini 3.1 Flash Lite Image, Gemini 3 Pro Image): <c>generateContent</c>,
/// <c>responseModalities: ["IMAGE"]</c>, <c>imageConfig.aspectRatio</c> (istenen boyuta en yakın oran), isteğe bağlı tohum.
/// Çağrı başına bir görsel; adet kadar tekrarlanır. Yanıttaki <c>inlineData</c> base64 görselleri çözülür; maliyet görsel başı.
/// </summary>
public sealed class GeminiImageModel : HttpImageModelBase
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="options">Zamanlama ayarları.</param>
    public GeminiImageModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http, MediaModelOptions options)
        : base(model, credential, dependencies, http, options)
    {
    }

    /// <inheritdoc />
    protected override Task<Result<ImageModelOutput>> GenerateCoreAsync(ImageRequest request, CancellationToken ct) =>
        RepeatAsync(request.Count, c => GenerateOneAsync(request, c), ct);

    /// <summary>Tek görsel üretir.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="ct">İptal belirteci.</param>
    private async Task<Result<ImageModelOutput>> GenerateOneAsync(ImageRequest request, CancellationToken ct)
    {
        var config = new JsonObject
        {
            ["responseModalities"] = new JsonArray("IMAGE"),
            ["imageConfig"] = new JsonObject
            {
                ["aspectRatio"] = ImageSizes.NearestAspectRatio(request.Width, request.Height, ImageSizes.CommonAspectRatios),
            },
        };
        if (request.Seed is { } seed)
        {
            config["seed"] = (int)(Math.Abs(seed) % int.MaxValue);
        }

        var response = await SendAsync(GeminiProtocol.CreateRequest(ModelId, Credential.ApiKey, PromptWithNegative(request), config), ct);
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

            var images = parsed.Value.Parts.Where(p => p.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)).ToList();
            return images.Count == 0
                ? Error.Failure(ProviderErrorCodes.EmptyResponse, $"{PlatformDisplayName} görsel döndürmedi.")
                : Result.Success(Output(images.Select(i => i.Data).ToList(), images[0].MimeType));
        });
    }
}
