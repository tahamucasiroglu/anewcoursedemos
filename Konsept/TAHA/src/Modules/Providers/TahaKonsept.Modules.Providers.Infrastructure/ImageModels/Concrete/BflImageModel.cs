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
/// Black Forest Labs FLUX (FLUX.3 Image Klein): <c>POST https://api.bfl.ai/v1/{model}</c> (<c>x-key</c>), genişlik/yükseklik
/// 16'nın katı; yanıt <c>{id, polling_url}</c>; <c>polling_url</c> sorgulanır: <c>Ready</c> → <c>result.sample</c> URL'si indirilir,
/// <c>Content/Request Moderated</c> → <c>providers.refused</c>, <c>Error</c>/<c>Failed</c> → <c>providers.bad_request</c>.
/// Çağrı başına bir görsel; adet kadar tekrarlanır.
/// </summary>
public sealed class BflImageModel : PollingImageModelBase
{
    /// <summary>API kökü.</summary>
    public static readonly Uri BaseUri = new("https://api.bfl.ai/v1/");

    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    /// <param name="options">Zamanlama ayarları.</param>
    public BflImageModel(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http, MediaModelOptions options)
        : base(model, credential, dependencies, http, options)
    {
    }

    /// <inheritdoc />
    protected override Task<Result<ImageModelOutput>> GenerateCoreAsync(ImageRequest request, CancellationToken ct) =>
        RepeatAsync(request.Count, c => RunJobAsync(request, c), ct);

    /// <inheritdoc />
    protected override async Task<Result<Uri>> SubmitAsync(ImageRequest request, CancellationToken ct)
    {
        var (width, height) = ImageSizes.Fit(request.Width, request.Height, 16);
        var body = new JsonObject
        {
            ["prompt"] = PromptWithNegative(request),
            ["width"] = width,
            ["height"] = height,
            ["output_format"] = "png",
        };
        if (request.Seed is { } seed)
        {
            body["seed"] = seed;
        }

        var message = MediaHttp.Json(HttpMethod.Post, new Uri(BaseUri, ModelId), body);
        ApplyAuth(message);
        var response = await SendAsync(message, ct);
        if (response.IsFailure)
        {
            return response.Error;
        }

        return ReadJson(response.Value, root =>
        {
            var polling = MediaJson.Text(root, "polling_url");
            var id = MediaJson.Text(root, "id");
            if (!string.IsNullOrWhiteSpace(polling) && Uri.TryCreate(polling, UriKind.Absolute, out var uri))
            {
                return Result.Success(uri);
            }

            return string.IsNullOrWhiteSpace(id)
                ? Error.Failure(ProviderErrorCodes.EmptyResponse, $"{PlatformDisplayName} iş kimliği döndürmedi.")
                : Result.Success(new Uri(BaseUri, "get_result?id=" + Uri.EscapeDataString(id)));
        });
    }

    /// <inheritdoc />
    protected override void ApplyAuth(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Headers.TryAddWithoutValidation("x-key", Credential.ApiKey);
    }

    /// <inheritdoc />
    protected override ImagePollStatus ParseStatus(JsonElement root)
    {
        var status = MediaJson.Text(root, "status") ?? string.Empty;
        switch (status)
        {
            case "Ready":
                var sample = MediaJson.Path(root, "result", "sample") is { ValueKind: JsonValueKind.String } s ? s.GetString() : null;
                return ImagePollStatus.Done(string.IsNullOrEmpty(sample) ? [] : [sample]);
            case "Content Moderated":
            case "Request Moderated":
                return ImagePollStatus.Fail(ProviderErrorCodes.Refused, "içerik denetimine takıldı (" + status + ").");
            case "Error":
            case "Failed":
            case "Task not found":
                return ImagePollStatus.Fail(ProviderErrorCodes.BadRequest, status + " " + MediaHttp.ExtractErrorMessage(root.ToString()));
            default:
                return ImagePollStatus.Pending;
        }
    }
}
