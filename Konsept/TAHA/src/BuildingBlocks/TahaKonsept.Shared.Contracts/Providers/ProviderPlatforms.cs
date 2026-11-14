namespace TahaKonsept.Shared.Contracts.Providers;

/// <summary>
/// API anahtarı tutulan online platformların sabit kimlikleri (katalogdaki <c>platform</c> alanı ve
/// <c>api/providers/keys/{platform}</c> rota parçası). Yerel modeller için <see cref="Local"/>, video motorları için <see cref="Engine"/>.
/// </summary>
public static class ProviderPlatforms
{
    /// <summary>Anthropic (Claude).</summary>
    public const string Anthropic = "anthropic";

    /// <summary>OpenAI (GPT, gpt-image, gpt-4o-mini-tts).</summary>
    public const string OpenAi = "openai";

    /// <summary>OpenRouter (çok sağlayıcılı LLM geçidi).</summary>
    public const string OpenRouter = "openrouter";

    /// <summary>Z.ai (GLM, GLM-Image, CogView).</summary>
    public const string Zai = "zai";

    /// <summary>Moonshot (Kimi).</summary>
    public const string Moonshot = "moonshot";

    /// <summary>ElevenLabs (online ses).</summary>
    public const string ElevenLabs = "elevenlabs";

    /// <summary>Google Gemini (Nano Banana, Gemini TTS).</summary>
    public const string Google = "google";

    /// <summary>Azure Speech (Neural TTS; bölge de gerekir).</summary>
    public const string AzureSpeech = "azure-speech";

    /// <summary>Cartesia (Sonic).</summary>
    public const string Cartesia = "cartesia";

    /// <summary>fal.ai (online görsel).</summary>
    public const string Fal = "fal";

    /// <summary>Black Forest Labs (FLUX).</summary>
    public const string Bfl = "bfl";

    /// <summary>Replicate (online görsel).</summary>
    public const string Replicate = "replicate";

    /// <summary>Masaüstü ajanında çalışan yerel TTS/görsel modelleri (anahtar gerekmez).</summary>
    public const string Local = "local";

    /// <summary>Masaüstü ajanında çalışan video motorları (anahtar gerekmez).</summary>
    public const string Engine = "engine";

    /// <summary>Anahtar tutulabilen tüm online platformlar (Ayarlar sırası).</summary>
    public static IReadOnlyList<string> Online { get; } =
    [
        Anthropic, OpenAi, OpenRouter, Zai, Moonshot, Google, ElevenLabs, AzureSpeech, Cartesia, Fal, Bfl, Replicate,
    ];
}
