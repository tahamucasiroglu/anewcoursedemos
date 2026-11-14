using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Base;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;

/// <summary>
/// fal.ai görsel modelleri (Z-Image Turbo, FLUX.2 Flash): eşzamanlı uç <c>POST https://fal.run/{model}</c>
/// (<c>Authorization: Key …</c>), <c>image_size {width,height}</c> (16'nın katı), <c>num_images</c>, <c>sync_mode: true</c>
/// (görseller yanıtta data URI olarak gelir; URL gelirse indirilir). Maliyet görsel başı (katalog ~1080×1920 için).
/// </summary>
public sealed class FalImageModel : HttpImageModelBase
{
    /// <summary>Eşzamanlı uç kökü.</summary>
    public static readonly Uri BaseUri = new("https://fal.run/");

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="options">Zamanlama ayarları.</param>
    public FalImageModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http, MediaModelOptions options)
        : base(model, credential, dependencies, http, options)
    {
    }

    /// <inheritdoc />
    protected override async Task<Result<ImageModelOutput>> GenerateCoreAsync(ImageRequest request, CancellationToken ct)
    {
        var (width, height) = ImageSizes.Fit(request.Width, request.Height, 16);
        var body = new JsonObject
        {
            ["prompt"] = PromptWithNegative(request),
            ["image_size"] = new JsonObject { ["width"] = width, ["height"] = height },
            ["num_images"] = Math.Clamp(request.Count, 1, 4),
            ["output_format"] = "png",
            ["sync_mode"] = true,
            ["enable_safety_checker"] = true,
        };
        if (request.Seed is { } seed)
        {
            body["seed"] = seed;
        }

        var message = MediaHttp.Json(HttpMethod.Post, new Uri(BaseUri, ModelId), body);
        message.Headers.Authorization = new AuthenticationHeaderValue("Key", Credential.ApiKey);
        var response = await SendAsync(message, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        var urls = ReadJson(response.Value, root => Result.Success<IReadOnlyList<string>>(
            MediaJson.Items(root, "images").Select(i => MediaJson.Text(i, "url")).OfType<string>().ToList()));
        return urls.IsFailure ? urls.Error : await DownloadAllAsync(urls.Value, ct);
    }
}
