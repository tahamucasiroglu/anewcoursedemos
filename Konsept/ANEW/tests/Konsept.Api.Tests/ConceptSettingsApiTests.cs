using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Domain.Entities;
using Konsept.Modules.AiGateway.Domain.Enums;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Konsept ayarları, Api tarafı (spec 0005 AC-1–AC-6, AC-8; BR-1, BR-13): profil başına model seçimleri, efor kuralı, seçilebilir
/// model listesi (metin + yetkili + geçerli key), arayüzü atlayan isteğin reddi (kasıtlı bozma hedefi), ince ayar, kelime denetimi
/// ve sahiplik. Bellek depoları; gerçek PostgreSQL <see cref="ProfilesIntegrationTests"/>'te. Gerçek sağlayıcı çağrısı yok.
/// </summary>
public sealed class ConceptSettingsApiTests : IClassFixture<ConceptSettingsApiFactory>
{
    private const string Opus = "anthropic/claude-opus-4-5";
    private const string Sonnet = "anthropic/claude-sonnet-4-5";
    private readonly ConceptSettingsApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ConceptSettingsApiTests"/> class.</summary>
    /// <param name="factory">Fabrika.</param>
    public ConceptSettingsApiTests(ConceptSettingsApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>AC-1/AC-4: yeni profilde üç rol seçilmedi, ince ayarın her grubu Belirtilmedi, kelime denetimi kapalı; key yoksa seçilebilir model yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_NewProfile_Defaults()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileApiTests.CreateAsync(client, "Yeni Kanal", ct);

        var settings = await GetAsync(client, profile, ct);

        settings.GetProperty("profileId").GetGuid().ShouldBe(profile);
        settings.GetProperty("roles").EnumerateArray().Select(r => r.GetProperty("role").GetString()).ShouldBe(["concept", "dedupe", "topic"]);
        foreach (var role in settings.GetProperty("roles").EnumerateArray())
        {
            role.GetProperty("modelId").ValueKind.ShouldBe(JsonValueKind.Null);
            role.GetProperty("effort").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        settings.GetProperty("tone").ValueKind.ShouldBe(JsonValueKind.Null);
        settings.GetProperty("audience").ValueKind.ShouldBe(JsonValueKind.Null);
        settings.GetProperty("narration").ValueKind.ShouldBe(JsonValueKind.Null);
        settings.GetProperty("wordCheck").GetBoolean().ShouldBeFalse();
        settings.GetProperty("models").GetArrayLength().ShouldBe(0);
    }

    /// <summary>
    /// AC-3: yalnızca geçerli key'i olan sağlayıcıların ve yetkili olunan metin modelleri listelenir; "Key yok" (GLM), "Key okunamıyor"
    /// (Kimi), yetkisiz (Sonnet, GPT-5) ve görsel model (gpt-image-1) listelenmez; yerel/Ollama katalogda hiç yok.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_SelectableModels_OnlyTextWithKeyAndPermission()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = RestrictedFactory();
        using var client = await factory.CreateUserClientAsync(ct);
        var profile = await ProfileApiTests.CreateAsync(client, "Kanal", ct);
        await SetupKeysAsync(factory, client, ct);

        var settings = await GetAsync(client, profile, ct);
        var models = settings.GetProperty("models").EnumerateArray().ToList();

        models.Select(m => m.GetProperty("id").GetString()).ShouldBe([Opus, "openai/gpt-5-mini"]);
        models[0].GetProperty("name").GetString().ShouldBe("Claude Opus 4.5");
        models[0].GetProperty("effortLevels").EnumerateArray().Select(e => e.GetString()).ShouldBe(["low", "medium", "high"]);
        models[0].GetProperty("defaultEffort").GetString().ShouldBe("high");
        models.ShouldAllBe(m => !m.GetProperty("id").GetString()!.Contains("ollama", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// AC-3 (BR-13, kasıtlı bozma hedefi): yetkisiz, key'siz, key'i okunamayan, görsel ya da katalogda olmayan modeli seçmeye yönelik
    /// doğrudan istek 400 ile reddedilir ve kayıtlı seçim değişmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_UnusableModel_DirectRequestRejected_Unchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = RestrictedFactory();
        using var client = await factory.CreateUserClientAsync(ct);
        var profile = await ProfileApiTests.CreateAsync(client, "Kanal", ct);
        await SetupKeysAsync(factory, client, ct);
        (await PutAsync(client, profile, new { models = new[] { Role("concept", Opus, "medium") } }, ct)).EnsureSuccessStatusCode();

        foreach (var model in new[] { Sonnet, "openai/gpt-5", "glm/glm-4.6", "kimi/kimi-k2", "openai/gpt-image-1", "ollama/llama3" })
        {
            using var response = await PutAsync(client, profile, new { models = new[] { Role("concept", model, null) }, wordCheck = true }, ct);
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, model);
        }

        var settings = await GetAsync(client, profile, ct);
        RoleOf(settings, "concept").GetProperty("modelId").GetString().ShouldBe(Opus);
        RoleOf(settings, "concept").GetProperty("effort").GetString().ShouldBe("medium");
        settings.GetProperty("wordCheck").GetBoolean().ShouldBeFalse();
    }

    /// <summary>
    /// AC-2: model seçilince katalog varsayılan eforu gelir, efor desteklemeyen modelde efor yoktur; seçim kalıcıdır (yenileme) ve
    /// şerit için model adı döner; efor değiştirilebilir; rol boşaltılabilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_SaveModels_PersistedWithDefaultEffort()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileApiTests.CreateAsync(client, "Kanal", ct);
        await PutKeyAsync(client, "anthropic", ct);

        using var save = await PutAsync(client, profile, new { models = new[] { Role("concept", Opus, null), Role("dedupe", Sonnet, null), Role("topic", null, null) } }, ct);
        var saved = await save.Content.ReadFromJsonAsync<JsonElement>(ct);
        var reloaded = await GetAsync(client, profile, ct);

        save.StatusCode.ShouldBe(HttpStatusCode.OK);
        foreach (var settings in new[] { saved, reloaded })
        {
            RoleOf(settings, "concept").GetProperty("modelId").GetString().ShouldBe(Opus);
            RoleOf(settings, "concept").GetProperty("modelName").GetString().ShouldBe("Claude Opus 4.5");
            RoleOf(settings, "concept").GetProperty("effort").GetString().ShouldBe("high");
            RoleOf(settings, "dedupe").GetProperty("modelName").GetString().ShouldBe("Claude Sonnet 4.5");
            RoleOf(settings, "dedupe").GetProperty("effort").ValueKind.ShouldBe(JsonValueKind.Null);
            RoleOf(settings, "topic").GetProperty("modelId").ValueKind.ShouldBe(JsonValueKind.Null);
        }

        (await PutAsync(client, profile, new { models = new[] { Role("concept", Opus, "low") } }, ct)).EnsureSuccessStatusCode();
        (await PutAsync(client, profile, new { models = new[] { Role("dedupe", null, null) } }, ct)).EnsureSuccessStatusCode();
        var after = await GetAsync(client, profile, ct);
        RoleOf(after, "concept").GetProperty("effort").GetString().ShouldBe("low");
        RoleOf(after, "dedupe").GetProperty("modelId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>AC-2: efor yalnızca modelin desteklediği düzeylerden biri; desteklemeyen modelde efor verilemez; bilinmeyen/tekrarlı rol reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_Effort_OnlySupportedLevels()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileApiTests.CreateAsync(client, "Kanal", ct);
        await PutKeyAsync(client, "anthropic", ct);

        using var max = await PutAsync(client, profile, new { models = new[] { Role("concept", Opus, "max") } }, ct);
        using var noEffortModel = await PutAsync(client, profile, new { models = new[] { Role("concept", Sonnet, "high") } }, ct);
        using var badRole = await PutAsync(client, profile, new { models = new[] { Role("video", Opus, null) } }, ct);
        using var twice = await PutAsync(client, profile, new { models = new[] { Role("concept", Opus, null), Role("concept", Sonnet, null) } }, ct);

        max.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        noEffortModel.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        badRole.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        twice.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        RoleOf(await GetAsync(client, profile, ct), "concept").GetProperty("modelId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>AC-4: profil A'daki model, ince ayar ve kelime denetimi değişikliği profil B'yi etkilemez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_PerProfile_Isolated()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var a = await ProfileApiTests.CreateAsync(client, "A", ct);
        var b = await ProfileApiTests.CreateAsync(client, "B", ct);
        await PutKeyAsync(client, "anthropic", ct);
        (await PutAsync(client, b, new { fineTune = new { tone = "calm", audience = (string?)null, narration = (string?)null } }, ct)).EnsureSuccessStatusCode();

        (await PutAsync(client, a, new { models = new[] { Role("topic", Opus, null) }, fineTune = new { tone = "dramatic", audience = "expert", narration = "list" }, wordCheck = true }, ct))
            .EnsureSuccessStatusCode();

        var settingsA = await GetAsync(client, a, ct);
        var settingsB = await GetAsync(client, b, ct);
        RoleOf(settingsA, "topic").GetProperty("modelId").GetString().ShouldBe(Opus);
        settingsA.GetProperty("tone").GetString().ShouldBe("dramatic");
        settingsA.GetProperty("wordCheck").GetBoolean().ShouldBeTrue();
        RoleOf(settingsB, "topic").GetProperty("modelId").ValueKind.ShouldBe(JsonValueKind.Null);
        settingsB.GetProperty("tone").GetString().ShouldBe("calm");
        settingsB.GetProperty("audience").ValueKind.ShouldBe(JsonValueKind.Null);
        settingsB.GetProperty("wordCheck").GetBoolean().ShouldBeFalse();
    }

    /// <summary>AC-5: her grupta tek değer saklanır, kalıcıdır, "Belirtilmedi"ye (null) dönülebilir; listede olmayan değer reddedilir ve kayıt değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_FineTune_SingleChoicePersisted_InvalidRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileApiTests.CreateAsync(client, "Kanal", ct);

        (await PutAsync(client, profile, new { fineTune = new { tone = "mysterious", audience = "family", narration = "what_if" } }, ct)).EnsureSuccessStatusCode();
        (await PutAsync(client, profile, new { fineTune = new { tone = "calm", audience = "family", narration = "what_if" } }, ct)).EnsureSuccessStatusCode();
        using var invalid = await PutAsync(client, profile, new { fineTune = new { tone = "angry", audience = "family", narration = "what_if" } }, ct);
        using var array = await PutAsync(client, profile, new { fineTune = new { tone = new[] { "calm", "dramatic" } } }, ct);
        var kept = await GetAsync(client, profile, ct);

        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        array.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        kept.GetProperty("tone").GetString().ShouldBe("calm");
        kept.GetProperty("audience").GetString().ShouldBe("family");
        kept.GetProperty("narration").GetString().ShouldBe("what_if");

        (await PutAsync(client, profile, new { fineTune = new { tone = (string?)null, audience = "family", narration = "what_if" } }, ct)).EnsureSuccessStatusCode();
        (await GetAsync(client, profile, ct)).GetProperty("tone").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>AC-6: kelime denetimi işaretlenip kaldırıldığında durum kaydedilir ve yeniden okununca korunur; diğer ayarlar değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_WordCheck_Persisted()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var profile = await ProfileApiTests.CreateAsync(client, "Kanal", ct);
        (await PutAsync(client, profile, new { fineTune = new { tone = "calm", audience = (string?)null, narration = (string?)null } }, ct)).EnsureSuccessStatusCode();

        (await PutAsync(client, profile, new { wordCheck = true }, ct)).EnsureSuccessStatusCode();
        var on = await GetAsync(client, profile, ct);
        (await PutAsync(client, profile, new { wordCheck = false }, ct)).EnsureSuccessStatusCode();
        var off = await GetAsync(client, profile, ct);

        on.GetProperty("wordCheck").GetBoolean().ShouldBeTrue();
        off.GetProperty("wordCheck").GetBoolean().ShouldBeFalse();
        off.GetProperty("tone").GetString().ShouldBe("calm");
    }

    /// <summary>AC-8 (BR-1): A, B'nin profiline ait ayarı okuyamaz ve değiştiremez ("bulunamadı"); B'nin ayarı değişmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_OtherUsersProfile_NotFound_Unchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        using var a = await _factory.CreateUserClientAsync(ct);
        using var b = await _factory.CreateUserClientAsync(ct);
        var profileB = await ProfileApiTests.CreateAsync(b, "B Kanalı", ct);
        (await PutAsync(b, profileB, new { wordCheck = true }, ct)).EnsureSuccessStatusCode();

        using var read = await a.GetAsync(new Uri($"/api/profiles/{profileB}/concept-settings", UriKind.Relative), ct);
        using var write = await PutAsync(a, profileB, new { wordCheck = false, fineTune = new { tone = "calm", audience = (string?)null, narration = (string?)null } }, ct);
        var body = await read.Content.ReadAsStringAsync(ct);

        read.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        write.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        body.ShouldContain("Profil bulunamadı");
        var kept = await GetAsync(b, profileB, ct);
        kept.GetProperty("wordCheck").GetBoolean().ShouldBeTrue();
        kept.GetProperty("tone").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>AC-8: girişsiz istekler reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptSettings_Anonymous_Unauthorized()
    {
        var ct = TestContext.Current.CancellationToken;
        using var anonymous = _factory.CreateClient();
        var id = Guid.NewGuid();

        using var read = await anonymous.GetAsync(new Uri($"/api/profiles/{id}/concept-settings", UriKind.Relative), ct);
        using var write = await anonymous.PutAsJsonAsync($"/api/profiles/{id}/concept-settings", new { wordCheck = true }, ct);

        read.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        write.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static object Role(string role, string? modelId, string? effort) => new { role, modelId, effort };

    private static JsonElement RoleOf(JsonElement settings, string role) =>
        settings.GetProperty("roles").EnumerateArray().Single(r => r.GetProperty("role").GetString() == role);

    private static async Task<JsonElement> GetAsync(HttpClient client, Guid profile, CancellationToken ct) =>
        await client.GetFromJsonAsync<JsonElement>($"/api/profiles/{profile}/concept-settings", ct);

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, Guid profile, object body, CancellationToken ct) =>
        client.PutAsJsonAsync($"/api/profiles/{profile}/concept-settings", body, ct);

    private static async Task PutKeyAsync(HttpClient client, string provider, CancellationToken ct) =>
        (await client.PutAsJsonAsync($"/api/ai/keys/{provider}", new { apiKey = "sk-test-" + Guid.NewGuid().ToString("N") }, ct)).EnsureSuccessStatusCode();

    // Yetki: Opus, GPT-5 mini, gpt-image-1, Kimi, GLM (Sonnet ve GPT-5 yetkisiz).
    private static ConceptSettingsApiFactory RestrictedFactory() => new()
    {
        UserModelPermissions = "anthropic/claude-opus-4-5,openai/gpt-5-mini,openai/gpt-image-1,kimi/kimi-k2,glm/glm-4.6",
    };

    // Key: Anthropic ve OpenAI geçerli; Kimi okunamaz (çözülemeyen şifreli metin); GLM yok.
    private static async Task SetupKeysAsync(ConceptSettingsApiFactory factory, HttpClient client, CancellationToken ct)
    {
        await PutKeyAsync(client, "anthropic", ct);
        await PutKeyAsync(client, "openai", ct);
        var owner = factory.Keys.Rows.First().UserId;
        await factory.Keys.UpsertAsync(
            new UserApiKey
            {
                Id = Guid.NewGuid(),
                UserId = owner,
                ProviderId = "kimi",
                Ciphertext = "okunamaz-sifreli-metin",
                Hint = "…abcd",
                Validation = KeyValidation.Verified,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            ct);
    }
}
