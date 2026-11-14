using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Base;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;

/// <summary>
/// Z.ai görsel modelleri (GLM-Image, CogView-4): <c>POST https://api.z.ai/api/paas/v4/images/generations</c> (Bearer),
/// <c>size</c> = <c>GxY</c> (16'nın katı, en fazla 2^21 piksel); yanıt <c>data[].url</c> indirilir. Çağrı başına bir görsel;
/// adet kadar tekrarlanır. Maliyet görsel başı.
/// </summary>
public sealed class ZaiImageModel : HttpImageModelBase
{
    /// <summary>API adresi.</summary>
    public static readonly Uri Endpoint = new("https://api.z.ai/api/paas/v4/images/generations");

    /// <summary>En fazla piksel (CogView-4 sınırı).</summary>
    private const long MaxPixels = 1L << 21;

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="options">Zamanlama ayarları.</param>
    public ZaiImageModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http, MediaModelOptions options)
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
        var (width, height) = ImageSizes.Fit(request.Width, request.Height, 16, MaxPixels);
        var body = new JsonObject
        {
            ["model"] = ModelId,
            ["prompt"] = PromptWithNegative(request),
            ["size"] = $"{width}x{height}",
        };
        var message = MediaHttp.Json(HttpMethod.Post, Endpoint, body);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Credential.ApiKey);
        var response = await SendAsync(message, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        var urls = ReadJson(response.Value, root => Result.Success<IReadOnlyList<string>>(
            MediaJson.Items(root, "data").Select(d => MediaJson.Text(d, "url")).OfType<string>().ToList()));
        return urls.IsFailure ? urls.Error : await DownloadAllAsync(urls.Value, ct);
    }
}
