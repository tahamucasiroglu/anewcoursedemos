using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Base;
using TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.ImageModels.Concrete;

/// <summary>
/// Replicate görsel modelleri (FLUX schnell): <c>POST v1/models/{sahip/ad}/predictions</c> (Bearer),
/// <c>input {prompt, aspect_ratio, num_outputs (1-4), output_format: png, seed}</c>; yanıttaki <c>urls.get</c> sorgulanır:
/// <c>succeeded</c> → <c>output</c> URL'leri indirilir, <c>failed</c>/<c>canceled</c> → <c>providers.bad_request</c>.
/// </summary>
public sealed class ReplicateImageModel : PollingImageModelBase
{
    /// <summary>API kökü.</summary>
    public static readonly Uri BaseUri = new("https://api.replicate.com/v1/");

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="options">Zamanlama ayarları.</param>
    public ReplicateImageModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http, MediaModelOptions options)
        : base(model, credential, dependencies, http, options)
    {
    }

    /// <inheritdoc />
    protected override async Task<Result<Uri>> SubmitAsync(ImageRequest request, CancellationToken ct)
    {
        var input = new JsonObject
        {
            ["prompt"] = PromptWithNegative(request),
            ["aspect_ratio"] = ImageSizes.NearestAspectRatio(request.Width, request.Height, ImageSizes.CommonAspectRatios),
            ["num_outputs"] = Math.Clamp(request.Count, 1, 4),
            ["output_format"] = "png",
        };
        if (request.Seed is { } seed)
        {
            input["seed"] = seed;
        }

        var message = MediaHttp.Json(HttpMethod.Post, new Uri(BaseUri, $"models/{ModelId}/predictions"), new JsonObject { ["input"] = input });
        ApplyAuth(message);
        var response = await SendAsync(message, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        return ReadJson(response.Value, root =>
        {
            var get = MediaJson.Path(root, "urls", "get") is { ValueKind: JsonValueKind.String } g ? g.GetString() : null;
            return !string.IsNullOrWhiteSpace(get) && Uri.TryCreate(get, UriKind.Absolute, out var uri)
                ? Result.Success(uri)
                : Error.Failure(ProviderErrorCodes.EmptyResponse, $"{PlatformDisplayName} tahmin adresi döndürmedi.");
        });
    }

    /// <inheritdoc />
    protected override void ApplyAuth(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Credential.ApiKey);
    }

    /// <inheritdoc />
    protected override ImagePollStatus ParseStatus(JsonElement root)
    {
        var status = MediaJson.Text(root, "status");
        switch (status)
        {
            case "succeeded":
                var output = MediaJson.Path(root, "output");
                IReadOnlyList<string> urls = output switch
                {
                    { ValueKind: JsonValueKind.String } single => [single.GetString()!],
                    { ValueKind: JsonValueKind.Array } many => many.EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!)
                        .ToList(),
                    _ => [],
                };
                return ImagePollStatus.Done(urls);
            case "failed":
            case "canceled":
                return ImagePollStatus.Fail(ProviderErrorCodes.BadRequest, status + ": " + (MediaJson.Text(root, "error") ?? "ayrıntı yok"));
            default:
                return ImagePollStatus.Pending;
        }
    }
}
