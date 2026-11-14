using Shouldly;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Catalog;

/// <summary>
/// Gömülü katalog: PLAN §1'deki tüm modeller var, alanlar tutarlı, süzme ve OpenRouter geçiş kimliği çalışıyor.
/// </summary>
public sealed class ModelCatalogTests
{
    /// <summary>PLAN §1'deki tüm anahtarlar.</summary>
    public static readonly string[] RequiredKeys =
    [
        "claude-opus-5-5", "claude-sonnet-5-5", "claude-haiku-5-5", "claude-fable-5-2", "gpt-6.1-sol", "gpt-6-luna", "openrouter", "glm-5.3", "kimi-k3",
        "gpt-4o-mini-tts", "elevenlabs-v4", "elevenlabs-v4-turbo", "gemini-3.8-flash-tts", "azure-neural", "cartesia-sonic-3.6",
        "gemini-nano-banana-2.1", "gemini-3.1-flash-lite-image", "gemini-3-pro-image", "gpt-image-2", "gpt-image-1-mini", "fal-z-image-turbo",
        "fal-flux-2-flash", "bfl-flux-3-image-klein", "glm-image", "cogview-4", "replicate-flux-schnell",
        "supertonic-3", "chatterbox-multilingual", "ema-lightning", "ahenk", "ahenk-cep", "trendyol-tts", "xtts-v2", "f5-tts-tr", "piper-tr",
        "mms-tts-tr", "omnivoice", "voxcpm2", "fish-s2-pro", "kokoro",
        "z-image-turbo", "flux2-klein-4b", "flux2-klein-9b", "flux3-klein", "sd35-medium", "sdxl-lightning", "qwen-image",
        "hyperframes", "remotion", "revideo",
    ];

    /// <summary>Tüm zorunlu modeller katalogda.</summary>
    [Fact]
    public void Contains_every_model_from_the_plan()
    {
        var missing = RequiredKeys.Where(k => TestFixtures.Catalog.Find(k) is null).ToList();
        missing.ShouldBeEmpty();
        TestFixtures.Catalog.All.Select(m => m.Key).ShouldBeUnique();
    }

    /// <summary>Online modeller anahtar tutulan bir platformda, yereller ajanda.</summary>
    [Fact]
    public void Platforms_and_run_locations_are_consistent()
    {
        foreach (var model in TestFixtures.Catalog.All)
        {
            if (model.RunsOn == ModelRunsOn.Server)
            {
                ProviderPlatforms.Online.ShouldContain(model.Platform, model.Key);
            }
            else
            {
                new[] { ProviderPlatforms.Local, ProviderPlatforms.Engine }.ShouldContain(model.Platform, model.Key);
            }
        }
    }

    /// <summary>Online metin modellerinde fiyat ve çıkış sınırı tanımlı (OpenRouter hariç: maliyet yanıtta).</summary>
    [Fact]
    public void Text_models_have_prices()
    {
        var text = TestFixtures.Catalog.All.Where(m => m.Capability == ModelCapability.Text).ToList();
        text.Count.ShouldBe(10);
        foreach (var model in text.Where(m => m.Platform != ProviderPlatforms.OpenRouter))
        {
            model.Pricing.InputPerMTok.ShouldNotBeNull(model.Key);
            model.Pricing.OutputPerMTok.ShouldNotBeNull(model.Key);
        }

        TestFixtures.Model("openrouter").Pricing.ProviderReportsCost.ShouldBeTrue();
        TestFixtures.Model("claude-opus-5-5").MaxOutputTokens.ShouldBe(128000);
    }

    /// <summary>Dil etiketleri: Sadece Türkçe modeller ve Kokoro.</summary>
    [Fact]
    public void Language_labels_match_the_plan()
    {
        foreach (var key in new[] { "ema-lightning", "ahenk", "ahenk-cep", "trendyol-tts", "piper-tr", "mms-tts-tr" })
        {
            TestFixtures.Model(key).LanguageLabel.ShouldBe(LanguageLabels.TurkishOnly);
        }

        TestFixtures.Model("kokoro").LanguageLabel.ShouldBe(LanguageLabels.NoTurkish);
        TestFixtures.Model("voxcpm2").MinVramMb.ShouldBe(8192);
    }

    /// <summary>Yetenek süzgeci.</summary>
    [Fact]
    public void Query_filters_by_capability_and_location()
    {
        var engines = TestFixtures.Catalog.Query(ModelCapability.VideoEngine, null).Value;
        engines.Select(m => m.Key).ShouldBe(["hyperframes", "remotion", "revideo"]);
        TestFixtures.Catalog.Query(ModelCapability.Speech, ModelRunsOn.Server).Value.Count.ShouldBe(6);
    }

    /// <summary>OpenRouter geçiş kimliği.</summary>
    [Fact]
    public void Find_supports_openrouter_passthrough_ids()
    {
        var model = TestFixtures.Catalog.Find("openrouter:anthropic/claude-sonnet-5.5");
        model.ShouldNotBeNull();
        model.Platform.ShouldBe(ProviderPlatforms.OpenRouter);
        model.ProviderModelId.ShouldBe("anthropic/claude-sonnet-5.5");
        model.Key.ShouldBe("openrouter:anthropic/claude-sonnet-5.5");
    }
}
