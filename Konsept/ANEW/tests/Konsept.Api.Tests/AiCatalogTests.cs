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
        var sonnet = seed["models"]!.AsArray().Single(m => (string?)m!["id"] == "anthropic/claude-sonnet-5-5")!;
        sonnet["outputTokenLimit"] = 12345;
        sonnet["timeoutSeconds"] = 77;
        sonnet["inputPricePerMillionUsd"] = 0.123456m;
        await using var factory = new AiGatewayApiFactory { Catalog = EmbeddedCatalogSource.Parse(seed.ToJsonString()) };
        using var client = await factory.CreateUserClientAsync(ct);

        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);

        var model = catalog.GetProperty("models").EnumerateArray().Single(m => m.GetProperty("id").GetString() == "anthropic/claude-sonnet-5-5");
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

    /// <summary>
    /// Spec 0026 AC-1: katalog API'si güncel online modelleri spec tablosundaki API adı, efor, varsayılan efor, çıktı limiti ve fiyatla
    /// döner; kaldırılan eski kimlikler listede yoktur; yerel girişler değişmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Catalog_CurrentOnlineModels_MatchSpecTable()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        string[] all = ["low", "medium", "high", "max"];
        var expected = new (string Id, string Api, string[] Efforts, string? Default, int? Limit, decimal? In, decimal? Out, decimal? Image)[]
        {
            ("anthropic/claude-opus-5-5", "claude-opus-5-5", all, "medium", 128000, 4m, 20m, null),
            ("anthropic/claude-sonnet-5-5", "claude-sonnet-5-5", all, "high", 128000, 2m, 10m, null),
            ("anthropic/claude-haiku-5-5", "claude-haiku-5-5", all, "medium", 128000, 0.10m, 0.50m, null),
            ("openai/gpt-6-astra", "gpt-6-astra", all, "medium", 128000, 10m, 50m, null),
            ("openai/gpt-6.1-sol", "gpt-6.1-sol", all, "medium", 128000, 2m, 10m, null),
            ("openai/gpt-6.1-terra", "gpt-6.1-terra", all, "medium", 128000, 1m, 5m, null),
            ("openai/gpt-image-2.6-flare", "gpt-image-2.6-flare", [], null, null, null, null, 0.044m),
            ("openrouter/anthropic/claude-sonnet-5.5", "anthropic/claude-sonnet-5.5", [], null, 128000, 2m, 10m, null),
            ("openrouter/anthropic/claude-opus-5.5", "anthropic/claude-opus-5.5", [], null, 128000, 4m, 20m, null),
            ("glm/glm-5.3", "glm-5.3", [], null, 128000, 1.40m, 4.40m, null),
            ("kimi/kimi-k3.1-pro", "kimi-k3.1-pro", [], null, 131072, 2m, 10m, null),
        };

        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);
        var online = catalog.GetProperty("models").EnumerateArray().Where(m => !m.GetProperty("local").GetBoolean()).ToList();
        var seed = new EmbeddedCatalogSource().Catalog;

        online.Select(m => m.GetProperty("id").GetString()).ShouldBe(expected.Select(e => e.Id));
        foreach (var e in expected)
        {
            var model = online.Single(m => m.GetProperty("id").GetString() == e.Id);
            seed.Models.Single(m => m.Id == e.Id).ProviderModelName.ShouldBe(e.Api, e.Id);
            model.GetProperty("effortLevels").EnumerateArray().Select(x => x.GetString()).ShouldBe(e.Efforts, e.Id);
            Value(model, "defaultEffort").ShouldBe(e.Default, e.Id);
            Value(model, "outputTokenLimit").ShouldBe(e.Limit is { } limit ? (decimal)limit : null, e.Id);
            Value(model, "inputPricePerMillionUsd").ShouldBe(e.In, e.Id);
            Value(model, "outputPricePerMillionUsd").ShouldBe(e.Out, e.Id);
            Value(model, "pricePerImageUsd").ShouldBe(e.Image, e.Id);
        }

        var ids = catalog.GetProperty("models").EnumerateArray().Select(m => m.GetProperty("id").GetString()).ToList();
        seed.ReplacedModels.Keys.ShouldAllBe(old => !ids.Contains(old));
        catalog.GetProperty("models").EnumerateArray().Where(m => m.GetProperty("local").GetBoolean()).Select(m => m.GetProperty("id").GetString())
            .ShouldBe(["local/z-image-turbo", "local/flux2-klein-4b", "local/supertonic-3", "local/chatterbox-multilingual-v3", "local/hyperframes", "local/remotion"]);
    }

    /// <summary>
    /// Spec 0026 AC-2: her yerine geçen eşlemesi katalogda var olan, eski modelin türünde, kendisi yerine geçilmemiş bir girişe işaret
    /// eder; eski kimlik katalog girişi değildir; <see cref="Modules.AiGateway.Domain.Entities.CatalogSeed.Find"/> eski kimliği yeni
    /// girişe çözer. Profiles ve Production veri migration'larının eşlemesi katalogla aynıdır (plan 0026, AC-4 taşıması).
    /// </summary>
    [Fact]
    public void ReplacedModels_PointToCurrentSameKindEntries()
    {
        var seed = new EmbeddedCatalogSource().Catalog;
        var expected = seed.ReplacedModels.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (p.Key, p.Value)).ToList();

        expected.Count.ShouldBe(9);
        foreach (var (oldId, newId) in expected)
        {
            seed.Models.ShouldNotContain(m => m.Id == oldId, oldId);
            var entry = seed.Models.SingleOrDefault(m => m.Id == newId);
            entry.ShouldNotBeNull(newId);
            entry.Local.ShouldBeFalse(newId);
            entry.ProviderId.ShouldBe(oldId[..oldId.IndexOf('/', StringComparison.Ordinal)], oldId);
            entry.Kind.ShouldBe(oldId.Contains("gpt-image", StringComparison.Ordinal) ? ModelKind.Image : ModelKind.Text, oldId);
            seed.ReplacedModels.ContainsKey(newId).ShouldBeFalse(newId);
            seed.Find(oldId).ShouldBeSameAs(entry);
            seed.Find(newId).ShouldBeSameAs(entry);
        }

        seed.Find("openai/gpt-4").ShouldBeNull();
        seed.Find(null).ShouldBeNull();
        Konsept.Modules.Profiles.Infrastructure.Persistence.Migrations.ReplaceRetiredModels.Replaced.OrderBy(r => r.Old, StringComparer.Ordinal).ShouldBe(expected);
        Konsept.Modules.Production.Infrastructure.Persistence.Migrations.ReplaceRetiredModels.Replaced.OrderBy(r => r.Old, StringComparer.Ordinal).ShouldBe(expected);
    }

    private static object? Value(JsonElement model, string name)
    {
        var value = model.GetProperty(name);
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Number => value.GetDecimal(),
            _ => value.GetString(),
        };
    }
}
