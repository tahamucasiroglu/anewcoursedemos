using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Application.Base;
using Konsept.Modules.AiGateway.Application.Concrete;
using Konsept.Modules.AiGateway.Application.Dtos;
using Konsept.Modules.AiGateway.Domain.Enums;
using Konsept.Modules.AiGateway.Infrastructure.Catalog;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Katalog (spec 0004 AC-1, AC-2, AC-16 Api tarafı): seed'deki sağlayıcı ve modeller, her modelin zorunlu alanları, yerel/ses/TTS
/// girişinin olmaması, değerlerin yalnızca seed'den gelmesi ve kullanıcıya göre kullanılabilirlik etiketi.
/// </summary>
public sealed class AiCatalogTests : IClassFixture<AiGatewayApiFactory>
{
    private static readonly string[] ExpectedProviders = ["anthropic", "openai", "openrouter", "glm", "kimi"];
    private readonly AiGatewayApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AiCatalogTests"/> class.</summary>
    /// <param name="factory">AiGateway test fabrikası.</param>
    public AiCatalogTests(AiGatewayApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// AC-1: beş online sağlayıcı; her metin sağlayıcısında 1–3 metin modeli; tek online görsel model OpenAI gpt-image; her modelde
    /// sağlayıcı, ad, tür, efor seviyeleri, varsayılan efor, çıktı token limiti (metin), zaman aşımı ve USD fiyatı; Ollama/yerel LLM,
    /// ses türü, TTS, fal ve Google görsel girişi yok.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Catalog_Seed_ProvidersModelsAndFields()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);

        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);

        catalog.GetProperty("providers").EnumerateArray().Select(p => p.GetProperty("id").GetString()).ShouldBe(ExpectedProviders);
        // Plan 0006 A-1: 0006 Req 18 kataloğa yerel girişleri ve "ses"/"video motoru" türlerini ekler; 0004'ün online iddiaları online
        // girişlere aynen uygulanır, alan iddiaları tüm girişlere.
        var all = catalog.GetProperty("models").EnumerateArray().ToList();
        var models = all.Where(m => !m.GetProperty("local").GetBoolean()).ToList();
        models.Select(m => m.GetProperty("kind").GetString()).Distinct().Order(StringComparer.Ordinal).ShouldBe(["image", "text"]);
        foreach (var provider in ExpectedProviders)
        {
            models.Count(m => m.GetProperty("providerId").GetString() == provider && m.GetProperty("kind").GetString() == "text").ShouldBeInRange(1, 3, provider);
        }

        var images = models.Where(m => m.GetProperty("kind").GetString() == "image").ToList();
        images.ShouldHaveSingleItem().GetProperty("providerId").GetString().ShouldBe("openai");
        images[0].GetProperty("name").GetString()!.ShouldStartWith("gpt-image");
        images[0].GetProperty("pricePerImageUsd").GetDecimal().ShouldBeGreaterThan(0m);

        foreach (var model in all)
        {
            var id = model.GetProperty("id").GetString()!;
            model.GetProperty("name").GetString().ShouldNotBeNullOrWhiteSpace(id);
            model.GetProperty("providerName").GetString().ShouldNotBeNullOrWhiteSpace(id);
            model.GetProperty("timeoutSeconds").GetInt32().ShouldBe(600, id);
            var efforts = model.GetProperty("effortLevels").EnumerateArray().Select(e => e.GetString()).ToList();
            efforts.ShouldAllBe(e => e == "low" || e == "medium" || e == "high" || e == "max");
            if (model.GetProperty("defaultEffort").ValueKind == JsonValueKind.String)
            {
                efforts.ShouldContain(model.GetProperty("defaultEffort").GetString());
            }

            if (model.GetProperty("kind").GetString() == "text")
            {
                model.GetProperty("outputTokenLimit").GetInt32().ShouldBeGreaterThan(0, id);
                model.GetProperty("inputPricePerMillionUsd").GetDecimal().ShouldBeGreaterThan(0m, id);
                model.GetProperty("outputPricePerMillionUsd").GetDecimal().ShouldBeGreaterThan(0m, id);
            }
        }

        var raw = EmbeddedCatalogSource.ReadEmbeddedJson();
        foreach (var forbidden in new[] { "ollama", "\"voice\"", "tts", "fal", "google", "nano" })
        {
            raw.ShouldNotContain(forbidden, Case.Insensitive);
        }

        foreach (var model in models)
        {
            model.GetProperty("kind").GetString().ShouldNotBe("audio");
            model.GetProperty("id").GetString()!.ShouldNotContain("local", Case.Insensitive);
            model.GetProperty("providerId").GetString()!.ShouldNotContain("local", Case.Insensitive);
        }
    }

    /// <summary>
    /// AC-2: bir modelin çıktı token limiti, zaman aşımı ve fiyatı yalnızca seed değiştirilerek değişir ve liste yeni değeri (sentin
    /// kesirleri dahil) gösterir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Catalog_ChangedSeed_ListShowsNewValues()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = JsonNode.Parse(EmbeddedCatalogSource.ReadEmbeddedJson())!;
        var sonnet = seed["models"]!.AsArray().Single(m => (string?)m!["id"] == "anthropic/claude-sonnet-4-5")!;
        sonnet["outputTokenLimit"] = 12345;
        sonnet["timeoutSeconds"] = 77;
        sonnet["inputPricePerMillionUsd"] = 0.123456m;
        await using var factory = new AiGatewayApiFactory { Catalog = EmbeddedCatalogSource.Parse(seed.ToJsonString()) };
        using var client = await factory.CreateUserClientAsync(ct);

        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);

        var model = catalog.GetProperty("models").EnumerateArray().Single(m => m.GetProperty("id").GetString() == "anthropic/claude-sonnet-4-5");
        model.GetProperty("outputTokenLimit").GetInt32().ShouldBe(12345);
        model.GetProperty("timeoutSeconds").GetInt32().ShouldBe(77);
        model.GetProperty("inputPricePerMillionUsd").GetDecimal().ShouldBe(0.123456m);
        model.GetProperty("inputPricePerMillionUsd").GetRawText().ShouldBe(0.123456m.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// AC-16 (Api): key'i olmayan sağlayıcının modelleri "no_key", key kaydedilen sağlayıcınınkiler "available"; etiket kullanıcıya
    /// özeldir (başka kullanıcının key'i etkilemez). Kimliksiz istek 401.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Catalog_Availability_PerUserKeys()
    {
        var ct = TestContext.Current.CancellationToken;
        using var owner = await _factory.CreateUserClientAsync(ct);
        using var other = await _factory.CreateUserClientAsync(ct);
        using var anonymous = _factory.CreateClient();

        (await Availability(owner, ct)).Values.ShouldAllBe(a => a == "no_key");
        (await owner.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = "sk-ant-" + Guid.NewGuid().ToString("N") }, ct)).EnsureSuccessStatusCode();

        var mine = await Availability(owner, ct);
        mine.Where(p => p.Key.StartsWith("anthropic/", StringComparison.Ordinal)).ShouldAllBe(p => p.Value == "available");
        mine.Where(p => !p.Key.StartsWith("anthropic/", StringComparison.Ordinal)).ShouldAllBe(p => p.Value == "no_key");
        (await Availability(other, ct)).Values.ShouldAllBe(a => a == "no_key");
        (await anonymous.GetAsync(new Uri("/api/ai/catalog", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<Dictionary<string, string>> Availability(HttpClient client, CancellationToken ct)
    {
        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);
        return catalog.GetProperty("models").EnumerateArray()
            .Where(m => !m.GetProperty("local").GetBoolean()) // Plan 0006 A-1: key etiketi yalnızca online girişlerde (yereller AC-23'te).
            .ToDictionary(m => m.GetProperty("id").GetString()!, m => m.GetProperty("availability").GetString()!, StringComparer.Ordinal);
    }

    /// <summary>
    /// Spec 0006 AC-23: Z-Image-Turbo ve FLUX.2 klein 4B "görsel", Supertonic 3 ve Chatterbox Multilingual V3 "ses", HyperFrames ve Remotion
    /// "video motoru" türünde yerel girişlerdir; tür listesi dört türdür; key'i hiç olmayan kullanıcıda yerel girişler "Key yok" almaz ve
    /// birim fiyatları yoktur; Ollama/yerel LLM yoktur; Remotion "Lisans uyarısı" taşır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Catalog_LocalEntries_FourKinds_NoKeyLabel()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);

        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);

        var all = catalog.GetProperty("models").EnumerateArray().ToList();
        all.Select(m => m.GetProperty("kind").GetString()).Distinct().Order(StringComparer.Ordinal).ShouldBe(["audio", "image", "text", "video_engine"]);
        var local = all.Where(m => m.GetProperty("local").GetBoolean()).ToDictionary(m => m.GetProperty("name").GetString()!, StringComparer.Ordinal);
        local.Keys.Order(StringComparer.Ordinal).ShouldBe(
            ["Chatterbox Multilingual V3", "FLUX.2 klein 4B", "HyperFrames", "Remotion", "Supertonic 3", "Z-Image-Turbo"]);
        local["Z-Image-Turbo"].GetProperty("kind").GetString().ShouldBe("image");
        local["FLUX.2 klein 4B"].GetProperty("kind").GetString().ShouldBe("image");
        local["Supertonic 3"].GetProperty("kind").GetString().ShouldBe("audio");
        local["Chatterbox Multilingual V3"].GetProperty("kind").GetString().ShouldBe("audio");
        local["HyperFrames"].GetProperty("kind").GetString().ShouldBe("video_engine");
        local["Remotion"].GetProperty("kind").GetString().ShouldBe("video_engine");
        local["Remotion"].GetProperty("licenseWarning").GetBoolean().ShouldBeTrue();
        local["HyperFrames"].GetProperty("licenseWarning").GetBoolean().ShouldBeFalse();
        foreach (var (name, model) in local)
        {
            model.GetProperty("availability").GetString().ShouldBe("available", name);
            model.GetProperty("pricePerImageUsd").ValueKind.ShouldBe(JsonValueKind.Null, name);
            model.GetProperty("inputPricePerMillionUsd").ValueKind.ShouldBe(JsonValueKind.Null, name);
            model.GetProperty("outputPricePerMillionUsd").ValueKind.ShouldBe(JsonValueKind.Null, name);
        }

        all.Where(m => !m.GetProperty("local").GetBoolean()).ShouldAllBe(m => m.GetProperty("availability").GetString() == "no_key");
        all.ShouldAllBe(m => !m.GetProperty("name").GetString()!.Contains("Ollama", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// B-89: metin modeli dizini (Konseptler modalı) yalnızca "metin" türünü verir; gerçek seed'deki ses, video motoru ve görsel girişleri
    /// (yerel ve online) listede yoktur; kullanılabilirlik yalnızca "available" iken seçilebilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TextDirectory_OnlyTextKind()
    {
        var ct = TestContext.Current.CancellationToken;
        var seed = new EmbeddedCatalogSource().Catalog;
        var dto = new CatalogDto(seed.Version, [], [.. seed.Models.Select(m => new CatalogModelDto(
            m.Id, m.ProviderId, m.ProviderId, m.Name, CatalogServiceBase.KindOf(m.Kind), m.EffortLevels, m.DefaultEffort, null, 600, null, null, null,
            CatalogModelDto.Available, m.Local))]);

        var listed = await new TextModelDirectory(new StubCatalogService(dto)).ListAsync(ct);

        listed.Select(m => m.Id).ShouldBe(seed.Models.Where(m => m.Kind == ModelKind.Text).Select(m => m.Id));
        listed.ShouldAllBe(m => m.Selectable);
        seed.Models.Count(m => m.Kind is ModelKind.Audio or ModelKind.VideoEngine).ShouldBe(4);
    }
}
