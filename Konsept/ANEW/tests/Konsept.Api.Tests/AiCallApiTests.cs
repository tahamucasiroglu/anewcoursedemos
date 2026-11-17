using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Domain.Enums;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Deneme çağrısı ucu ve model yetkisi uçtan uca (spec 0013 AC-2, AC-1, AC-9; plan 0013 Risk 3), sahte sağlayıcıyla: yetki ve key
/// kararı sunucudadır, arayüzü atlayan doğrudan istek de reddedilir; Modeller listesi yetkisiz modeli "Yetki yok" gösterir.
/// </summary>
public sealed class AiCallApiTests : IClassFixture<AiGatewayApiFactory>
{
    private const string Sonnet = "anthropic/claude-sonnet-5-5";
    private readonly AiGatewayApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="AiCallApiTests"/> class.</summary>
    /// <param name="factory">AiGateway test fabrikası (varsayılan: tüm modeller yetkili).</param>
    public AiCallApiTests(AiGatewayApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// AC-2: rol yapılandırması kullanıcıyı yalnızca bir modele daraltınca başka bir modelle doğrudan istek, key kayıtlı olsa bile
    /// sağlayıcıya gitmeden "Bu modeli kullanma yetkiniz yok" ile reddedilir; katalogda o model "no_permission"; yetkili modelde key
    /// yoksa AC-1 mesajı.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trial_RestrictedModel_DirectRequestRejected_CatalogShowsNoPermission()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AiGatewayApiFactory { UserModelPermissions = "openai/gpt-6.1-terra" };
        using var client = await factory.CreateUserClientAsync(ct);
        (await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = ApiKeyApiTests.NewKey("sk-ant-") }, ct)).EnsureSuccessStatusCode();

        using var denied = await client.PostAsJsonAsync("/api/ai/calls/trial", new { modelId = Sonnet }, ct);
        using var noKey = await client.PostAsJsonAsync("/api/ai/calls/trial", new { modelId = "openai/gpt-6.1-terra" }, ct);
        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);

        denied.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await denied.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("Bu modeli kullanma yetkiniz yok");
        (await noKey.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("title").GetString().ShouldBe("OpenAI için API key girilmemiş — Ayarlar");
        factory.Provider.Calls.ShouldBeEmpty();
        factory.Spend.Records.ShouldBeEmpty();
        var availability = catalog.GetProperty("models").EnumerateArray()
            .ToDictionary(m => m.GetProperty("id").GetString()!, m => m.GetProperty("availability").GetString(), StringComparer.Ordinal);
        availability[Sonnet].ShouldBe("no_permission");
        availability["openai/gpt-6.1-terra"].ShouldBe("no_key");
        availability.Where(p => p.Key != "openai/gpt-6.1-terra").ShouldAllBe(p => p.Value == "no_permission");
    }

    /// <summary>
    /// AC-2 (varsayılan) + AC-9: varsayılan kullanıcı rolü her modeli çağırabilir; deneme çağrısı kullanıcının key'iyle gider, sonuç
    /// metni ve maliyeti döner, kayıt "deneme" amacıyla yazılır; aylık toplam artar. Kimliksiz istek 401.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trial_DefaultUser_AnyModel_RecordsSpend()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await _factory.CreateUserClientAsync(ct, email);
        var userId = _factory.Users.Get(email).Id;
        var key = ApiKeyApiTests.NewKey("sk-ant-");
        (await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = key }, ct)).EnsureSuccessStatusCode();
        using var anonymous = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/ai/calls/trial", new { modelId = Sonnet }, ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var month = await client.GetFromJsonAsync<JsonElement>("/api/ai/spend/month", ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("text").GetString().ShouldNotBeNullOrWhiteSpace();
        body.GetProperty("costUsd").GetDecimal().ShouldBe(0.00054m); // 120 × 2 / 1M + 30 × 10 / 1M (Sonnet 5.5; plan 0026 A-1)
        var call = _factory.Provider.Calls.Last(c => c.Call.ApiKey == key).Call;
        call.MaxOutputTokens.ShouldBe(4096);
        var record = _factory.Spend.Records.Single(r => r.UserId == userId);
        record.Purpose.ShouldBe(CallPurpose.Trial);
        month.GetProperty("totalUsd").GetDecimal().ShouldBe(0.00054m);
        (await anonymous.PostAsJsonAsync("/api/ai/calls/trial", new { modelId = Sonnet }, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync(new Uri("/api/ai/spend/month", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Deneme ucu bilinmeyen modeli 404, görsel modelini 400 ile reddeder (görsel maliyetli; deneme yalnızca metin).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trial_UnknownOrImageModel_Rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);

        (await client.PostAsJsonAsync("/api/ai/calls/trial", new { modelId = "yok/model" }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostAsJsonAsync("/api/ai/calls/trial", new { modelId = "openai/gpt-image-2.6-flare" }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/ai/calls/trial", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
