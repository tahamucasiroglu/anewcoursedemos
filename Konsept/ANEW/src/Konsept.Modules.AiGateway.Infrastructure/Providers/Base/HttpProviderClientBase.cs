using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Domain.Enums;

namespace Konsept.Modules.AiGateway.Infrastructure.Providers.Base;

/// <summary>
/// Sağlayıcılara tek deneme gönderen ham HTTP istemcisi (spec 0013, Y-7: dış LLM API'lerine yalnızca AiGateway gider). İstek biçimi
/// seed'deki <see cref="ProviderDialect"/>'e göre kurulur: Anthropic Messages (<c>x-api-key</c>, <c>max_tokens</c>,
/// <c>output_config.effort</c>), OpenAI Chat Completions (<c>max_completion_tokens</c>, <c>reasoning_effort</c>) ve Images,
/// OpenRouter (<c>max_tokens</c>, <c>usage.include</c> → bildirilen maliyet) ve diğer OpenAI uyumlu uçlar (GLM, Kimi). Kendisi tekrar
/// etmez, zaman aşımı uygulamaz (kapı yapar). Bağlantı hiç kurulamadıysa <see cref="ProviderAttempt.Unreachable"/>, gönderildikten sonra
/// koptuysa <see cref="ProviderAttempt.Lost"/>. Hata yanıtının gövdesi okunmaz ve dışarı verilmez (Req 9); HTTP istemcisinin logu kapalıdır.
/// </summary>
/// <param name="http">HTTP istemci fabrikası.</param>
public abstract class HttpProviderClientBase(IHttpClientFactory http) : IProviderClient
{
    /// <summary>Adlandırılmış HTTP istemcisi.</summary>
    public const string ClientName = "ai-calls";

    /// <summary>Çıktı sınırı verilmemiş (katalogda limitsiz) metin çağrısında Anthropic'e giden <c>max_tokens</c>.</summary>
    public const int FallbackMaxTokens = 4096;

    /// <summary>Görsel kalitesi (katalog fiyatıyla eşleşir).</summary>
    public const string ImageQuality = "medium";

    /// <summary>Görsel boyutu (katalog fiyatıyla eşleşir).</summary>
    public const string ImageSize = "1024x1024";

    private const string AnthropicVersion = "2023-06-01";

    /// <inheritdoc />
    public async Task<ProviderAttempt> SendAsync(ProviderCall call, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        var isImage = call.Model.Kind == ModelKind.Image;
        var url = isImage ? call.Provider.ImageUrl : call.Provider.ChatUrl;
        var body = isImage ? ImageBody(call) : ChatBody(call);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        if (call.Provider.AuthStyle == ProviderAuthStyle.Anthropic)
        {
            request.Headers.TryAddWithoutValidation("x-api-key", call.ApiKey);
            request.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", call.ApiKey);
        }

        using var client = http.CreateClient(ClientName);
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException e) when (IsConnectFailure(e))
        {
            return ProviderAttempt.Unreachable();
        }
        catch (HttpRequestException)
        {
            return ProviderAttempt.Lost();
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return ProviderAttempt.Failed((int)response.StatusCode);
            }

            try
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                await using (stream.ConfigureAwait(false))
                {
                    using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return isImage ? ParseImage(doc.RootElement) : ParseChat(call.Provider.Dialect, doc.RootElement);
                }
            }
            catch (HttpRequestException)
            {
                return ProviderAttempt.Lost();
            }
            catch (IOException)
            {
                return ProviderAttempt.Lost();
            }
            catch (JsonException)
            {
                return ProviderAttempt.Completed(null, null, null); // 2xx ama okunamayan gövde: token yok → tahmini kayıt.
            }
        }
    }

    /// <summary>Metin çağrısının istek gövdesi (sağlayıcı biçimine göre).</summary>
    /// <param name="call">Çağrı.</param>
    /// <returns>JSON.</returns>
    public static JsonObject ChatBody(ProviderCall call)
    {
        ArgumentNullException.ThrowIfNull(call);
        var model = call.Model.ProviderModelName;
        if (call.Provider.Dialect == ProviderDialect.Anthropic)
        {
            var anthropic = new JsonObject
            {
                ["model"] = model,
                ["max_tokens"] = call.MaxOutputTokens ?? FallbackMaxTokens,
                ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = call.Prompt }),
            };
            if (!string.IsNullOrEmpty(call.System))
            {
                anthropic["system"] = call.System;
            }

            if (call.Effort is { } effort)
            {
                anthropic["output_config"] = new JsonObject { ["effort"] = effort };
            }

            return anthropic;
        }

        var messages = new JsonArray();
        if (!string.IsNullOrEmpty(call.System))
        {
            messages.Add(new JsonObject { ["role"] = "system", ["content"] = call.System });
        }

        messages.Add(new JsonObject { ["role"] = "user", ["content"] = call.Prompt });
        var body = new JsonObject { ["model"] = model, ["messages"] = messages };
        var limitField = call.Provider.Dialect == ProviderDialect.OpenAi ? "max_completion_tokens" : "max_tokens";
        if (call.MaxOutputTokens is { } limit)
        {
            body[limitField] = limit;
        }

        if (call.Provider.Dialect == ProviderDialect.OpenAi && call.Effort is { } reasoning)
        {
            body["reasoning_effort"] = reasoning;
        }

        if (call.Provider.Dialect == ProviderDialect.OpenRouter)
        {
            body["usage"] = new JsonObject { ["include"] = true };
        }

        return body;
    }

    /// <summary>Görsel çağrısının istek gövdesi (OpenAI Images).</summary>
    /// <param name="call">Çağrı.</param>
    /// <returns>JSON.</returns>
    public static JsonObject ImageBody(ProviderCall call)
    {
        ArgumentNullException.ThrowIfNull(call);
        return new JsonObject
        {
            ["model"] = call.Model.ProviderModelName,
            ["prompt"] = call.Prompt,
            ["n"] = call.ImageCount,
            // Katalogdaki görsel fiyatı (0,042 USD) medium kalite 1024×1024 içindir; API varsayılanı "auto" daha pahalı kaliteyi
            // seçebilir, kayıtla fatura ayrışır (review r1 L-1).
            ["quality"] = ImageQuality,
            ["size"] = ImageSize,
        };
    }

    private static ProviderAttempt ParseChat(ProviderDialect dialect, JsonElement root)
    {
        if (dialect == ProviderDialect.Anthropic)
        {
            var text = new StringBuilder();
            if (root.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray())
                {
                    if (Str(block, "type") == "text" && Str(block, "text") is { } part)
                    {
                        text.Append(part);
                    }
                }
            }

            var usage = Obj(root, "usage");
            return ProviderAttempt.Completed(text.ToString(), Int(usage, "input_tokens"), Int(usage, "output_tokens"), Str(root, "stop_reason") == "max_tokens");
        }

        string? message = null;
        var truncated = false;
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            message = Str(Obj(first, "message"), "content");
            truncated = Str(first, "finish_reason") == "length";
        }

        var used = Obj(root, "usage");
        decimal? reported = null;
        if (dialect == ProviderDialect.OpenRouter && used is { } u && u.TryGetProperty("cost", out var cost) && cost.ValueKind == JsonValueKind.Number
            && cost.TryGetDecimal(out var value))
        {
            reported = value;
        }

        return ProviderAttempt.Completed(message, Int(used, "prompt_tokens"), Int(used, "completion_tokens"), truncated) with { ReportedCostUsd = reported };
    }

    private static ProviderAttempt ParseImage(JsonElement root)
    {
        var images = new List<string>();
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in data.EnumerateArray())
            {
                if (Str(item, "b64_json") is { } b64)
                {
                    images.Add(b64);
                }
            }
        }

        return ProviderAttempt.Completed(null, null, null) with { Images = images };
    }

    private static bool IsConnectFailure(HttpRequestException e) =>
        e.HttpRequestError is HttpRequestError.ConnectionError or HttpRequestError.NameResolutionError or HttpRequestError.SecureConnectionError
            or HttpRequestError.ProxyTunnelError
        || e.InnerException is SocketException;

    private static JsonElement? Obj(JsonElement? element, string name) =>
        element is { ValueKind: JsonValueKind.Object } e && e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? value : null;

    private static string? Str(JsonElement? element, string name) =>
        element is { ValueKind: JsonValueKind.Object } e && e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int? Int(JsonElement? element, string name) =>
        element is { ValueKind: JsonValueKind.Object } e && e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var number)
            ? number
            : null;
}
