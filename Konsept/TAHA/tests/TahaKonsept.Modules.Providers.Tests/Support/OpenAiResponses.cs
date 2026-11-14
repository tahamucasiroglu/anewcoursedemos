namespace TahaKonsept.Modules.Providers.Tests.Support;

/// <summary>
/// OpenAI Chat Completions biçiminde hazır yanıt gövdeleri.
/// </summary>
public static class OpenAiResponses
{
    /// <summary>Tek seçenekli tamamlama yanıtı.</summary>
    /// <param name="content">İçerik.</param>
    /// <param name="prompt">Giriş token'ı.</param>
    /// <param name="completion">Çıkış token'ı.</param>
    /// <param name="cached">Önbellek token'ı.</param>
    /// <param name="cost">Sağlayıcı maliyeti (null = yok).</param>
    /// <param name="finish">Bitiş nedeni.</param>
    public static string Completion(string content, int prompt = 1000, int completion = 200, int cached = 0, decimal? cost = null, string finish = "stop")
    {
        var escaped = System.Text.Json.JsonSerializer.Serialize(content);
        var costPart = cost is null ? string.Empty : ", \"cost\": " + cost.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return "{\"id\":\"c1\",\"model\":\"m\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":" + escaped +
            "},\"finish_reason\":\"" + finish + "\"}],\"usage\":{\"prompt_tokens\":" + prompt + ",\"completion_tokens\":" + completion +
            ",\"prompt_tokens_details\":{\"cached_tokens\":" + cached + "}" + costPart + "}}";
    }
}
