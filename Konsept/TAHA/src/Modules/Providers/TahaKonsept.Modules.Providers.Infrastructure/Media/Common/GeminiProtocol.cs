using System.Text.Json;
using System.Text.Json.Nodes;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Google Gemini <c>generateContent</c> protokolü (görsel ve ses modelleri ortak kullanır): istek
/// (<c>POST v1beta/models/{model}:generateContent</c>, <c>x-goog-api-key</c>, tek kullanıcı metni + <c>generationConfig</c>;
/// <see cref="CreateRequest"/>) ve yanıt çözümü (<see cref="Parse"/>: <c>inlineData</c> base64 parçaları, güvenlik engeli →
/// <c>providers.refused</c>, parça yoksa <c>providers.empty_response</c>, <c>usageMetadata</c> → token kullanımı).
/// </summary>
public static class GeminiProtocol
{
    /// <summary>API kökü.</summary>
    public static readonly Uri BaseUri = new("https://generativelanguage.googleapis.com/v1beta/");

    /// <summary>Güvenlik nedeniyle durma nedenleri.</summary>
    private static readonly HashSet<string> SafetyReasons = new(StringComparer.Ordinal)
    {
        "SAFETY", "PROHIBITED_CONTENT", "BLOCKLIST", "SPII", "IMAGE_SAFETY", "IMAGE_PROHIBITED_CONTENT", "RECITATION",
    };

    /// <summary>İsteği kurar.</summary>
    /// <param name="modelId">Sağlayıcı model kimliği.</param>
    /// <param name="apiKey">API anahtarı.</param>
    /// <param name="text">Kullanıcı metni (prompt ya da seslendirilecek metin).</param>
    /// <param name="generationConfig">Üretim ayarları (<c>responseModalities</c> vb.).</param>
    public static HttpRequestMessage CreateRequest(string modelId, string apiKey, string text, JsonObject generationConfig)
    {
        var body = new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray { new JsonObject { ["text"] = text } },
                },
            },
            ["generationConfig"] = generationConfig,
        };
        var request = MediaHttp.Json(HttpMethod.Post, new Uri(BaseUri, $"models/{Uri.EscapeDataString(modelId)}:generateContent"), body);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
        return request;
    }

    /// <summary>Yanıtı çözer.</summary>
    /// <param name="root">Yanıt kökü.</param>
    /// <param name="platformName">Görünen platform adı.</param>
    public static Result<GeminiOutput> Parse(JsonElement root, string platformName)
    {
        var blockReason = MediaJson.Path(root, "promptFeedback", "blockReason") is { ValueKind: JsonValueKind.String } b ? b.GetString() : null;
        if (!string.IsNullOrEmpty(blockReason))
        {
            return Error.Failure(ProviderErrorCodes.Refused, $"{platformName} isteği güvenlik nedeniyle engelledi ({blockReason}).");
        }

        var parts = new List<GeminiInlineData>();
        string? finishReason = null;
        var candidates = MediaJson.Items(root, "candidates").ToList();
        if (candidates.Count > 0)
        {
            finishReason = MediaJson.Text(candidates[0], "finishReason");
            var content = MediaJson.Path(candidates[0], "content") ?? default;
            foreach (var part in MediaJson.Items(content, "parts"))
            {
                var inline = MediaJson.Path(part, "inlineData") ?? MediaJson.Path(part, "inline_data");
                if (inline is { } data && MediaJson.Text(data, "data") is { Length: > 0 } base64)
                {
                    var mime = MediaJson.Text(data, "mimeType") ?? MediaJson.Text(data, "mime_type") ?? "application/octet-stream";
                    parts.Add(new GeminiInlineData(Convert.FromBase64String(base64), mime));
                }
            }
        }

        if (parts.Count == 0)
        {
            return finishReason is not null && SafetyReasons.Contains(finishReason)
                ? Error.Failure(ProviderErrorCodes.Refused, $"{platformName} çıktıyı güvenlik nedeniyle vermedi ({finishReason}).")
                : Error.Failure(ProviderErrorCodes.EmptyResponse, $"{platformName} boş yanıt döndü (durma nedeni: {finishReason ?? "yok"}).");
        }

        var usage = MediaJson.Path(root, "usageMetadata") ?? default;
        var tokens = new CallUsage
        {
            InputTokens = MediaJson.Number(usage, "promptTokenCount"),
            OutputTokens = MediaJson.Number(usage, "candidatesTokenCount") + MediaJson.Number(usage, "thoughtsTokenCount"),
        };
        return new GeminiOutput(parts, tokens);
    }
}
