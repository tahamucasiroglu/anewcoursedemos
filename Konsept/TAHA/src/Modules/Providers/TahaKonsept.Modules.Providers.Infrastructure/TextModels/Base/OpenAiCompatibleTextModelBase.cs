using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Application.TextModels.Base;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Infrastructure.TextModels.Base;

/// <summary>
/// OpenAI Chat Completions uyumlu sağlayıcıların ortak tabanı (OpenAI, OpenRouter, GLM, Kimi, Yerel LLM). <c>POST {BaseUri}chat/completions</c>,
/// Bearer anahtar, <c>response_format</c> (json_schema strict ya da json_object + şema talimatı), <c>reasoning_effort</c>,
/// akışta SSE (<c>stream_options.include_usage</c>) ve <c>usage</c> → <see cref="CallUsage"/> (önbellek token'ları ayrılır,
/// <c>usage.cost</c> varsa sağlayıcı maliyeti). Platform farkları sanal üyelerle (<see cref="ApplyEffort"/>, <see cref="ApplyWebSearch"/>…).
/// </summary>
public abstract class OpenAiCompatibleTextModelBase : TextModelBase
{
    /// <summary>Kurucu.</summary>
    /// <param name="model">Katalog kaydı.</param>
    /// <param name="credential">Kimlik bilgisi.</param>
    /// <param name="dependencies">Ortak bağımlılıklar.</param>
    /// <param name="http">HTTP istemcisi.</param>
    protected OpenAiCompatibleTextModelBase(ModelInfo model, ProviderCredential credential, AiModelDependencies dependencies, HttpClient http)
        : base(model, credential, dependencies)
    {
        Http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <summary>HTTP istemcisi.</summary>
    protected HttpClient Http { get; }

    /// <summary>API kök adresi (sonu <c>/</c> ile biter).</summary>
    protected abstract Uri BaseUri { get; }

    /// <summary>Çıkış sınırı alanının adı (<c>max_tokens</c>; OpenAI'da <c>max_completion_tokens</c>).</summary>
    protected virtual string MaxTokensField => "max_tokens";

    /// <summary>Yerel <c>json_schema</c> yanıt biçimi destekleniyor mu (değilse <c>json_object</c> + sistem talimatı).</summary>
    protected virtual bool SupportsJsonSchema => true;

    /// <inheritdoc />
    protected override async Task<Result<ModelOutput>> SendAsync(TextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var body = BuildRequestBody(request);
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(BaseUri, "chat/completions"))
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        ApplyAuthorization(message);
        if (request.Stream)
        {
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        }

        using var response = await Http.SendAsync(
            message,
            request.Stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
            ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            return ProviderHttpErrors.FromStatus(PlatformDisplayName, (int)response.StatusCode, ExtractErrorMessage(errorBody));
        }

        try
        {
            return request.Stream
                ? await ReadStreamAsync(response, ct)
                : ParseCompletion(await response.Content.ReadAsStringAsync(ct));
        }
        catch (JsonException ex)
        {
            return Error.Failure(ProviderErrorCodes.BadRequest, $"{PlatformDisplayName} yanıtı çözülemedi: {ex.Message}");
        }
    }

    /// <summary>Yetki başlığını ekler (varsayılan <c>Authorization: Bearer &lt;anahtar&gt;</c>).</summary>
    /// <param name="message">İstek.</param>
    protected virtual void ApplyAuthorization(HttpRequestMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Credential.ApiKey);
    }

    /// <summary>İstek gövdesini kurar.</summary>
    /// <param name="request">İstek.</param>
    protected virtual JsonObject BuildRequestBody(TextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var system = request.SystemPrompt;
        if (request.JsonSchema is not null && !SupportsJsonSchema)
        {
            system = AppendSchemaInstruction(system, request.JsonSchema);
        }

        var messages = new JsonArray();
        if (!string.IsNullOrWhiteSpace(system))
        {
            messages.Add(new JsonObject { ["role"] = "system", ["content"] = system });
        }

        messages.Add(new JsonObject { ["role"] = "user", ["content"] = request.UserPrompt });
        var body = new JsonObject
        {
            ["model"] = ProviderModelId,
            ["messages"] = messages,
            [MaxTokensField] = request.MaxOutputTokens,
        };

        ApplyEffort(body, ResolveEffort(request.Effort));
        if (request.JsonSchema is not null)
        {
            ApplyResponseFormat(body, request);
        }

        if (request.UseWebSearch)
        {
            ApplyWebSearch(body);
        }

        if (request.Stream)
        {
            body["stream"] = true;
            body["stream_options"] = new JsonObject { ["include_usage"] = true };
        }

        return body;
    }

    /// <summary>Efor alanını ekler (varsayılan <c>reasoning_effort</c>).</summary>
    /// <param name="body">Gövde.</param>
    /// <param name="effort">Efor (null = eklenmez).</param>
    protected virtual void ApplyEffort(JsonObject body, EffortLevel? effort)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (effort is { } value)
        {
            body["reasoning_effort"] = MapEffort(value);
        }
    }

    /// <summary>Efor seviyesini sağlayıcı değerine çevirir.</summary>
    /// <param name="effort">Efor.</param>
    protected virtual string MapEffort(EffortLevel effort) => effort switch
    {
        EffortLevel.Low => "low",
        EffortLevel.Medium => "medium",
        EffortLevel.High => "high",
        EffortLevel.XHigh => "xhigh",
        _ => "max",
    };

    /// <summary>Yanıt biçimini ekler.</summary>
    /// <param name="body">Gövde.</param>
    /// <param name="request">İstek (şema dolu).</param>
    protected virtual void ApplyResponseFormat(JsonObject body, TextRequest request)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(request);
        body["response_format"] = SupportsJsonSchema && request.JsonSchema is not null
            ? new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject
                {
                    ["name"] = request.SchemaName,
                    ["strict"] = true,
                    ["schema"] = request.JsonSchema.DeepClone(),
                },
            }
            : new JsonObject { ["type"] = "json_object" };
    }

    /// <summary>Web aramasını etkinleştirir (varsayılan: desteklenmez, hiçbir şey eklenmez).</summary>
    /// <param name="body">Gövde.</param>
    protected virtual void ApplyWebSearch(JsonObject body)
    {
    }

    /// <summary><c>usage</c> nesnesini okur.</summary>
    /// <param name="usage">Usage JSON'u.</param>
    protected virtual CallUsage ParseUsage(JsonElement usage)
    {
        var prompt = ReadLong(usage, "prompt_tokens");
        var completion = ReadLong(usage, "completion_tokens");
        var cached = usage.TryGetProperty("prompt_tokens_details", out var details) && details.ValueKind == JsonValueKind.Object
            ? ReadLong(details, "cached_tokens")
            : ReadLong(usage, "cached_tokens");
        decimal? cost = usage.TryGetProperty("cost", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetDecimal() : null;
        return new CallUsage
        {
            InputTokens = Math.Max(0, prompt - cached),
            OutputTokens = completion,
            CacheReadTokens = cached,
            ProviderCostUsd = cost,
        };
    }

    /// <summary>Akışsız yanıtı çözer.</summary>
    /// <param name="json">Yanıt gövdesi.</param>
    protected Result<ModelOutput> ParseCompletion(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        string? text = null;
        string? finish = null;
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            {
                text = content.GetString();
            }

            finish = ReadString(choice, "finish_reason");
        }

        var usage = root.TryGetProperty("usage", out var u) && u.ValueKind == JsonValueKind.Object ? ParseUsage(u) : CallUsage.Empty;
        return new ModelOutput(text ?? string.Empty, usage, finish, finish == "length", ReadString(root, "model"));
    }

    /// <summary>SSE akışını okur ve birleştirir.</summary>
    /// <param name="response">Yanıt.</param>
    /// <param name="ct">İptal belirteci.</param>
    protected async Task<Result<ModelOutput>> ReadStreamAsync(HttpResponseMessage response, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(response);
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var text = new StringBuilder();
        string? finish = null;
        string? modelId = null;
        var usage = CallUsage.Empty;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data == "[DONE]")
            {
                break;
            }

            if (data.Length == 0)
            {
                continue;
            }

            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                return ProviderHttpErrors.FromStatus(PlatformDisplayName, 500, error.ToString());
            }

            modelId ??= ReadString(root, "model");
            if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                {
                    text.Append(content.GetString());
                }

                finish = ReadString(choice, "finish_reason") ?? finish;
            }

            if (root.TryGetProperty("usage", out var u) && u.ValueKind == JsonValueKind.Object)
            {
                usage = ParseUsage(u);
            }
        }

        return new ModelOutput(text.ToString(), usage, finish, finish == "length", modelId);
    }

    /// <summary>Hata gövdesinden mesajı çıkarır (<c>{"error":{"message":…}}</c>).</summary>
    /// <param name="body">Gövde.</param>
    protected static string ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var error))
            {
                return error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var m)
                    ? m.ToString()
                    : error.ToString();
            }
        }
        catch (JsonException)
        {
            return body;
        }

        return body;
    }

    /// <summary>Sayısal alanı okur (yoksa 0).</summary>
    /// <param name="element">Nesne.</param>
    /// <param name="name">Alan.</param>
    protected static long ReadLong(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var n)
            ? n
            : 0;

    /// <summary>Metin alanı okur (yoksa null).</summary>
    /// <param name="element">Nesne.</param>
    /// <param name="name">Alan.</param>
    protected static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
