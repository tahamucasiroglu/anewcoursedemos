using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Modules.Providers.Application.Common;

/// <summary>
/// Anahtar tutulan online platformların tanımları (Ayarlar → API anahtarları satırları; sıra = görünüm sırası).
/// Önemli üyeler: <see cref="All"/>, <see cref="Find"/>, <see cref="DisplayNameOf"/>.
/// </summary>
public static class ProviderPlatformDirectory
{
    /// <summary>Tüm platformlar.</summary>
    public static IReadOnlyList<PlatformDescriptor> All { get; } =
    [
        new(ProviderPlatforms.Anthropic, "Anthropic (Claude)", "Konsept, video kodu, konu önerisi (varsayılan LLM)", false),
        new(ProviderPlatforms.OpenAi, "OpenAI", "GPT-6, gpt-image, gpt-4o-mini-tts", false),
        new(ProviderPlatforms.OpenRouter, "OpenRouter", "Çok sağlayıcılı LLM geçidi", false),
        new(ProviderPlatforms.Zai, "Z.ai (GLM)", "GLM-5.3, GLM-Image, CogView-4", false),
        new(ProviderPlatforms.Moonshot, "Moonshot (Kimi)", "Kimi K3", false),
        new(ProviderPlatforms.Google, "Google Gemini", "Nano Banana, Gemini görsel, Gemini Flash TTS", false),
        new(ProviderPlatforms.ElevenLabs, "ElevenLabs", "Online ses (v4 / v4 Turbo)", false),
        new(ProviderPlatforms.AzureSpeech, "Azure Speech", "Neural TTS (bölge gerekir, ör. westeurope)", true),
        new(ProviderPlatforms.Cartesia, "Cartesia", "Online ses (Sonic)", false),
        new(ProviderPlatforms.Fal, "fal.ai", "Online görsel (Z-Image Turbo, FLUX.2 Flash)", false),
        new(ProviderPlatforms.Bfl, "Black Forest Labs", "FLUX.3 Klein görsel", false),
        new(ProviderPlatforms.Replicate, "Replicate", "Online görsel (FLUX schnell)", false),
        new(
            ProviderPlatforms.LocalOpenAi,
            "Yerel LLM (OpenAI uyumlu)",
            "Ollama / LM Studio / llama.cpp server; kök adres gerekir (ör. http://localhost:11434/v1), anahtar isteğe bağlı. Model: local-openai:<model adı>",
            false,
            RequiresBaseUrl: true,
            KeyOptional: true),
    ];

    /// <summary>Platformu bulur (büyük/küçük harf duyarsız).</summary>
    /// <param name="platform">Platform kimliği.</param>
    public static PlatformDescriptor? Find(string? platform) =>
        string.IsNullOrWhiteSpace(platform)
            ? null
            : All.FirstOrDefault(p => string.Equals(p.Platform, platform.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Görünen ad (bilinmiyorsa kimliğin kendisi).</summary>
    /// <param name="platform">Platform kimliği.</param>
    public static string DisplayNameOf(string platform) => Find(platform)?.DisplayName ?? platform;
}
