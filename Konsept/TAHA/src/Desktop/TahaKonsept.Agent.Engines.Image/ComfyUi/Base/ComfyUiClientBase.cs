using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Engines.Image.ComfyUi.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Image.ComfyUi.Base;

/// <summary>
/// ComfyUI HTTP istemci tabanı: istek kurma, <c>/history</c> yanıtını <see cref="ComfyUiPromptStatus"/>'a çevirme
/// (<c>status.status_str</c>, <c>status.messages</c> içindeki <c>execution_error</c>, <c>outputs.*.images</c>).
/// Alt sınıf <see cref="CreateClient"/> ve <see cref="BaseUri"/> sağlar.
/// </summary>
public abstract class ComfyUiClientBase : IComfyUiClient
{
    private static readonly TimeSpan ReachabilityTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Taban oluşturur.</summary>
    /// <param name="logger">Günlükleyici.</param>
    protected ComfyUiClientBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>Günlükleyici.</summary>
    protected ILogger Logger { get; }

    /// <summary>Sunucu kök adresi (sonunda <c>/</c> ile).</summary>
    protected abstract Uri BaseUri { get; }

    /// <inheritdoc />
    public async Task<bool> IsReachableAsync(CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(ReachabilityTimeout);
            using var response = await CreateClient().GetAsync(new Uri(BaseUri, "system_stats"), timeout.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> QueuePromptAsync(JsonObject workflow, string clientId, CancellationToken ct)
    {
        var body = new JsonObject { ["prompt"] = workflow.DeepClone(), ["client_id"] = clientId };
        try
        {
            using var response = await CreateClient().PostAsJsonAsync(new Uri(BaseUri, "prompt"), body, ct).ConfigureAwait(false);
            var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogWarning("ComfyUI iş akışını reddetti ({Status}): {Body}", (int)response.StatusCode, text);
                return Error.Failure("comfyui.prompt.rejected", $"ComfyUI iş akışını reddetti (HTTP {(int)response.StatusCode}). Model dosyaları ComfyUI 'models' klasöründe mi? Ayrıntı: {Truncate(text)}");
            }

            var promptId = JsonNode.Parse(text)?["prompt_id"]?.GetValue<string>();
            return string.IsNullOrEmpty(promptId)
                ? Error.Failure("comfyui.prompt.noid", "ComfyUI prompt_id döndürmedi.")
                : promptId;
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure("comfyui.unreachable", $"ComfyUI'ye ulaşılamadı ({BaseUri}): {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ComfyUiPromptStatus?>> GetStatusAsync(string promptId, CancellationToken ct)
    {
        try
        {
            using var response = await CreateClient().GetAsync(new Uri(BaseUri, $"history/{Uri.EscapeDataString(promptId)}"), ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Error.Failure("comfyui.history.http", $"ComfyUI geçmişi okunamadı (HTTP {(int)response.StatusCode}).");
            }

            var root = JsonNode.Parse(await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false));
            return Result.Success(ParseHistory(promptId, root));
        }
        catch (HttpRequestException ex)
        {
            return Error.Failure("comfyui.unreachable", $"ComfyUI'ye ulaşılamadı ({BaseUri}): {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DownloadImageAsync(ComfyUiImageRef image, string destinationPath, CancellationToken ct)
    {
        var query = $"view?filename={Uri.EscapeDataString(image.FileName)}&subfolder={Uri.EscapeDataString(image.Subfolder)}&type={Uri.EscapeDataString(image.Type)}";
        try
        {
            using var response = await CreateClient().GetAsync(new Uri(BaseUri, query), HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure(Error.Failure("comfyui.view.http", $"ComfyUI görseli verilemedi (HTTP {(int)response.StatusCode}): {image.FileName}"));
            }

            await using var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var output = File.Create(destinationPath);
            await input.CopyToAsync(output, ct).ConfigureAwait(false);
            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure(Error.Failure("comfyui.unreachable", $"ComfyUI'ye ulaşılamadı ({BaseUri}): {ex.Message}"));
        }
    }

    /// <summary>İstemci.</summary>
    /// <returns>HTTP istemcisi.</returns>
    protected abstract HttpClient CreateClient();

    /// <summary><c>/history/{id}</c> yanıtını çözer; kayıt yoksa <c>null</c>.</summary>
    /// <param name="promptId">Kuyruk kimliği.</param>
    /// <param name="root">Yanıt kökü.</param>
    /// <returns>Durum ya da <c>null</c>.</returns>
    protected static ComfyUiPromptStatus? ParseHistory(string promptId, JsonNode? root)
    {
        if (root?[promptId] is not JsonObject entry)
        {
            return null;
        }

        var status = entry["status"];
        var statusText = status?["status_str"]?.GetValue<string>();
        var completed = status?["completed"]?.GetValue<bool>() ?? false;
        var failed = string.Equals(statusText, "error", StringComparison.OrdinalIgnoreCase);
        string? error = null;
        if (failed && status?["messages"] is JsonArray messages)
        {
            error = messages
                .OfType<JsonArray>()
                .Where(m => m.Count > 1 && m[0]?.GetValue<string>() == "execution_error")
                .Select(m => m[1]?["exception_message"]?.GetValue<string>())
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
        }

        var images = new List<ComfyUiImageRef>();
        if (entry["outputs"] is JsonObject outputs)
        {
            foreach (var output in outputs)
            {
                if (output.Value?["images"] is not JsonArray list)
                {
                    continue;
                }

                images.AddRange(list.OfType<JsonObject>().Select(i => new ComfyUiImageRef(
                    i["filename"]?.GetValue<string>() ?? string.Empty,
                    i["subfolder"]?.GetValue<string>() ?? string.Empty,
                    i["type"]?.GetValue<string>() ?? "output")));
            }
        }

        return new ComfyUiPromptStatus(completed || failed || images.Count > 0, failed, error, images);
    }

    private static string Truncate(string text) => text.Length <= 600 ? text : text[..600] + "…";
}
