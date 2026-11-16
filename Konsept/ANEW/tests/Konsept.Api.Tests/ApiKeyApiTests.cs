using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Konsept.Modules.AiGateway.Application.Abstract;
using Konsept.Modules.AiGateway.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// API key yönetimi, Api tarafı (spec 0004 AC-3, AC-4, AC-6–AC-11; H-2). Sağlayıcı doğrulaması sahtedir (gerçek çağrı yok); HTTP
/// eşlemesi <see cref="ProviderKeyValidatorTests"/>'te.
/// </summary>
public sealed class ApiKeyApiTests : IClassFixture<AiGatewayApiFactory>
{
    private readonly AiGatewayApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ApiKeyApiTests"/> class.</summary>
    /// <param name="factory">AiGateway test fabrikası.</param>
    public ApiKeyApiTests(AiGatewayApiFactory factory)
    {
        _factory = factory;
        _factory.Validator.Outcome = KeyCheckOutcome.Accepted;
    }

    /// <summary>
    /// AC-3: geçerli key kaydedilince yanıtta ve sonraki listede yalnızca maskeli hal (<c>ilk ≤3 … son 3</c>), "verified" ve
    /// güncellenme zamanı vardır; tam key ya da 8+ karakterlik parçası hiçbir yanıtta dönmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SaveKey_Valid_ResponseAndListOnlyMasked()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var key = NewKey("sk-ant-");

        using var save = await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = key }, ct);
        var saveText = await save.Content.ReadAsStringAsync(ct);
        var listText = await client.GetStringAsync(new Uri("/api/ai/keys", UriKind.Relative), ct);

        save.StatusCode.ShouldBe(HttpStatusCode.OK);
        var row = JsonDocument.Parse(saveText).RootElement;
        row.GetProperty("state").GetString().ShouldBe("saved");
        row.GetProperty("hint").GetString().ShouldBe("sk-…" + key[^3..]);
        row.GetProperty("validation").GetString().ShouldBe("verified");
        row.GetProperty("updatedAt").GetDateTimeOffset().ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));
        var listed = JsonDocument.Parse(listText).RootElement.EnumerateArray().Single(r => r.GetProperty("providerId").GetString() == "anthropic");
        listed.GetProperty("hint").GetString().ShouldBe("sk-…" + key[^3..]);
        ShouldNotLeak(key, saveText, listText);
    }

    /// <summary>
    /// AC-4: saklanan değer Data Protection şifresidir; girilen key'in hiçbir 8+ karakterlik parçası geçmez ve başka anahtar
    /// halkasıyla çözülemez (aynı halka çözer).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Protector_Ciphertext_HasNoPlainFragment_OtherKeyRingCannotRead()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, userId) = await NewUserAsync(ct);
        using var disposable = client;
        var key = NewKey("sk-proj-");
        (await client.PutAsJsonAsync("/api/ai/keys/openai", new { apiKey = key }, ct)).EnsureSuccessStatusCode();
        await using var other = _factory.WithOtherKeyRing();
        _ = other.Server;

        var stored = _factory.Keys.Rows.Single(r => r.UserId == userId && r.ProviderId == "openai");

        ShouldNotLeak(key, stored.Ciphertext, stored.Hint);
        _factory.Services.GetRequiredService<IApiKeyProtector>().CanUnprotect(stored.Ciphertext).ShouldBeTrue();
        other.Services.GetRequiredService<IApiKeyProtector>().CanUnprotect(stored.Ciphertext).ShouldBeFalse();
    }

    /// <summary>
    /// AC-6: boş, yalnızca boşluk, 8'den kısa, 512'den uzun ya da kontrol karakterli key Türkçe alan mesajıyla reddedilir; sağlayıcı
    /// çağrılmaz; mevcut key değişmez; mesaj girilen değeri içermez.
    /// </summary>
    /// <param name="value">Girilen değer.</param>
    /// <param name="message">Beklenen mesaj.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("", "API key'i girin.")]
    [InlineData("     ", "API key'i girin.")]
    [InlineData("abc1234", "API key en az 8 karakter olmalı.")]
    [InlineData("abcd\u0007efghij", "API key geçersiz karakter içeriyor.")]
    [InlineData("LONG", "API key en fazla 512 karakter olabilir.")]
    public async Task SaveKey_InvalidFormat_RejectedWithTurkishMessage(string value, string message)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var existing = NewKey("sk-");
        (await client.PutAsJsonAsync("/api/ai/keys/kimi", new { apiKey = existing }, ct)).EnsureSuccessStatusCode();
        var before = await Row(client, "kimi", ct);
        var calls = _factory.Validator.Calls.Count;
        var input = value == "LONG" ? new string('k', 513) : value;

        using var response = await client.PutAsJsonAsync("/api/ai/keys/kimi", new { apiKey = input }, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        FieldError(text).ShouldBe(message);
        _factory.Validator.Calls.Count.ShouldBe(calls);
        (await Row(client, "kimi", ct)).GetRawText().ShouldBe(before.GetRawText());
        if (input.Trim().Length > 0)
        {
            text.ShouldNotContain(input.Trim());
        }
    }

    /// <summary>AC-6: baş/son boşluklu key kırpılarak kaydedilir ve doğrulanır; sağlayıcı öneki olmayan 8+ karakterlik key reddedilmez.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SaveKey_Trimmed_NoPrefixRequired()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var key = "onekyok" + Guid.NewGuid().ToString("N")[..4];

        using var response = await client.PutAsJsonAsync("/api/ai/keys/glm", new { apiKey = "  \t" + key + "   " }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _factory.Validator.Calls.ShouldContain(("glm", key));
        (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("hint").GetString().ShouldBe(key[..2] + "…" + key[^3..]);
    }

    /// <summary>
    /// AC-7: "Değiştir" yeni key'i eskisinin yerine koyar — maske yeni key'e döner, doğrulama yeni key'le yapılır, sağlayıcı için tek
    /// satır kalır ve eski şifreli değer gider.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ReplaceKey_NewMaskAndValidationWithNewKey_SingleRow()
    {
        var ct = TestContext.Current.CancellationToken;
        var (client, userId) = await NewUserAsync(ct);
        using var disposable = client;
        var first = NewKey("sk-or-");
        var second = NewKey("sk-or-");
        (await client.PutAsJsonAsync("/api/ai/keys/openrouter", new { apiKey = first }, ct)).EnsureSuccessStatusCode();
        var firstCipher = _factory.Keys.Rows.Single(r => r.UserId == userId && r.ProviderId == "openrouter").Ciphertext;

        (await client.PutAsJsonAsync("/api/ai/keys/openrouter", new { apiKey = second }, ct)).EnsureSuccessStatusCode();

        (await Row(client, "openrouter", ct)).GetProperty("hint").GetString().ShouldBe("sk-…" + second[^3..]);
        _factory.Validator.Calls.ShouldContain(("openrouter", second));
        var rows = _factory.Keys.Rows.Where(r => r.UserId == userId && r.ProviderId == "openrouter").ToList();
        rows.ShouldHaveSingleItem().Ciphertext.ShouldNotBe(firstCipher);
    }

    /// <summary>AC-8 (Api): silinen key kalıcı gider; satır "none" olur ve o sağlayıcının modelleri "no_key".</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task DeleteKey_RowEmpty_ModelsNoKey()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var key = NewKey("sk-");
        (await client.PutAsJsonAsync("/api/ai/keys/openai", new { apiKey = key }, ct)).EnsureSuccessStatusCode();
        var count = _factory.Keys.Rows.Count;

        using var delete = await client.DeleteAsync(new Uri("/api/ai/keys/openai", UriKind.Relative), ct);

        delete.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await delete.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("state").GetString().ShouldBe("none");
        _factory.Keys.Rows.Count.ShouldBe(count - 1);
        var row = await Row(client, "openai", ct);
        row.GetProperty("state").GetString().ShouldBe("none");
        row.GetProperty("hint").ValueKind.ShouldBe(JsonValueKind.Null);
        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);
        catalog.GetProperty("models").EnumerateArray().Where(m => m.GetProperty("providerId").GetString() == "openai")
            .ShouldAllBe(m => m.GetProperty("availability").GetString() == "no_key");
    }

    /// <summary>
    /// AC-9: kullanıcı A — admin rolündeki kullanıcı dahil — kullanıcı B'nin key'ini (maskeli hali dahil) göremez, değiştiremez,
    /// silemez; A'nın işlemleri yalnızca kendi satırını etkiler.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ownership_OtherUserAndAdmin_CannotSeeOrChange()
    {
        var ct = TestContext.Current.CancellationToken;
        var (owner, ownerId) = await NewUserAsync(ct);
        using var disposable = owner;
        var ownerKey = NewKey("sk-ant-");
        (await owner.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = ownerKey }, ct)).EnsureSuccessStatusCode();
        var ownerRow = _factory.Keys.Rows.Single(r => r.UserId == ownerId && r.ProviderId == "anthropic");
        using var admin = _factory.Users.Users.Any(u => u.NormalizedEmail == IdentityApiFactory.AdminEmail.ToUpperInvariant())
            ? await _factory.SignInAsync(IdentityApiFactory.AdminEmail, ct)
            : await _factory.CreateUserClientAsync(ct, IdentityApiFactory.AdminEmail);
        using var stranger = await _factory.CreateUserClientAsync(ct);

        foreach (var other in new[] { admin, stranger })
        {
            var listText = await other.GetStringAsync(new Uri("/api/ai/keys", UriKind.Relative), ct);
            listText.ShouldNotContain(ownerRow.Hint);
            (await Row(other, "anthropic", ct)).GetProperty("state").GetString().ShouldBe("none");
            (await other.DeleteAsync(new Uri("/api/ai/keys/anthropic", UriKind.Relative), ct)).EnsureSuccessStatusCode();
            (await other.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = NewKey("sk-ant-") }, ct)).EnsureSuccessStatusCode();
        }

        _factory.Keys.Rows.Single(r => r.UserId == ownerRow.UserId && r.ProviderId == "anthropic").ShouldBe(ownerRow);
        (await Row(owner, "anthropic", ct)).GetProperty("hint").GetString().ShouldBe(ownerRow.Hint);
    }

    /// <summary>AC-9: kimliksiz istek key ve katalog uçlarına erişemez (401, veri yok).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Anonymous_KeyAndCatalogEndpoints_401()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var calls = _factory.Validator.Calls.Count;

        using var list = await client.GetAsync(new Uri("/api/ai/keys", UriKind.Relative), ct);
        using var catalog = await client.GetAsync(new Uri("/api/ai/catalog", UriKind.Relative), ct);
        using var save = await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = NewKey("sk-") }, ct);
        using var delete = await client.DeleteAsync(new Uri("/api/ai/keys/anthropic", UriKind.Relative), ct);

        foreach (var response in new[] { list, catalog, save, delete })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        _factory.Validator.Calls.Count.ShouldBe(calls);
    }

    /// <summary>
    /// AC-10: doğrulama sonucu etiketi belirler — kabul "verified", sonuçsuz (zaman aşımı, 429, 5xx, maliyetsiz yolu yok)
    /// "unverified" ile kaydedilir; her kayıtta doğrulama tam 1 kez çağrılır.
    /// </summary>
    /// <param name="outcome">Sahte doğrulama sonucu.</param>
    /// <param name="expected">Beklenen etiket.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData(KeyCheckOutcome.Accepted, "verified")]
    [InlineData(KeyCheckOutcome.Inconclusive, "unverified")]
    public async Task SaveKey_ValidatorOutcomes_StatusAndSingleCall(KeyCheckOutcome outcome, string expected)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var key = NewKey("sk-");
        _factory.Validator.Outcome = outcome;

        using var response = await client.PutAsJsonAsync("/api/ai/keys/openai", new { apiKey = key }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("validation").GetString().ShouldBe(expected);
        _factory.Validator.Calls.Count(c => c.Key == key).ShouldBe(1);
    }

    /// <summary>
    /// AC-10: sağlayıcı key'i reddederse (401) key kaydedilmez, alan mesajı "Sağlayıcı bu key'i reddetti" döner, önceki key değişmez,
    /// doğrulama tam 1 kez çağrılır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SaveKey_Rejected401_NotStored_PreviousKept()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var previous = NewKey("sk-ant-");
        (await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = previous }, ct)).EnsureSuccessStatusCode();
        var before = await Row(client, "anthropic", ct);
        var rows = _factory.Keys.Rows.Count;
        var rejected = NewKey("sk-ant-");
        _factory.Validator.Outcome = KeyCheckOutcome.Rejected;

        using var response = await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = rejected }, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        FieldError(text).ShouldBe("Sağlayıcı bu key'i reddetti");
        JsonDocument.Parse(text).RootElement.GetProperty("code").GetString().ShouldBe("ai.key_rejected");
        _factory.Validator.Calls.Count(c => c.Key == rejected).ShouldBe(1);
        _factory.Keys.Rows.Count.ShouldBe(rows);
        (await Row(client, "anthropic", ct)).GetRawText().ShouldBe(before.GetRawText());
        ShouldNotLeak(rejected, text);
    }

    /// <summary>
    /// AC-11: şifre çözme anahtarı değişmişse satır "unreadable" (maske yok) ve o sağlayıcının modelleri "unreadable" olur; yeni key
    /// girilince satır normale döner.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task UnreadableKey_ShowsUnreadable_ReplaceFixes()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using (var client = await _factory.CreateUserClientAsync(ct, email))
        {
            (await client.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = NewKey("sk-ant-") }, ct)).EnsureSuccessStatusCode();
        }

        await using var lostKeys = _factory.WithOtherKeyRing();
        using var again = await lostKeys.SignInAsync(email, ct);

        var row = await Row(again, "anthropic", ct);
        row.GetProperty("state").GetString().ShouldBe("unreadable");
        row.GetProperty("hint").ValueKind.ShouldBe(JsonValueKind.Null);
        var catalog = await again.GetFromJsonAsync<JsonElement>("/api/ai/catalog", ct);
        catalog.GetProperty("models").EnumerateArray().Where(m => m.GetProperty("providerId").GetString() == "anthropic")
            .ShouldAllBe(m => m.GetProperty("availability").GetString() == "unreadable");

        var fresh = NewKey("sk-ant-");
        (await again.PutAsJsonAsync("/api/ai/keys/anthropic", new { apiKey = fresh }, ct)).EnsureSuccessStatusCode();

        var fixedRow = await Row(again, "anthropic", ct);
        fixedRow.GetProperty("state").GetString().ShouldBe("saved");
        fixedRow.GetProperty("hint").GetString().ShouldBe("sk-…" + fresh[^3..]);
    }

    /// <summary>
    /// Review r1 M-1: yalnızca kalıcı kaydedilen key bilinen sır olur — sağlayıcının reddettiği değer süreç geneli maskeye girmez
    /// (log zehirleme / liste doldurma yok), kaydedilen öneksiz key ise her log metninde gizlenir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task KnownSecret_OnlyPersistedKeyRegistered()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var rejected = NewKey("glmred");
        var saved = NewKey("glmok");

        _factory.Validator.Outcome = KeyCheckOutcome.Rejected;
        (await client.PutAsJsonAsync("/api/ai/keys/glm", new { apiKey = rejected }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _factory.Validator.Outcome = KeyCheckOutcome.Inconclusive;
        (await client.PutAsJsonAsync("/api/ai/keys/glm", new { apiKey = saved }, ct)).EnsureSuccessStatusCode();

        Konsept.Shared.Kernel.Security.SecretMasker.Mask($"satır {rejected} son").ShouldContain(rejected);
        Konsept.Shared.Kernel.Security.SecretMasker.Mask($"satır {saved} son").ShouldNotContain(saved);
    }

    /// <summary>Katalogda olmayan sağlayıcı 404; doğrulama çağrılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SaveKey_UnknownProvider_404()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var calls = _factory.Validator.Calls.Count;

        using var response = await client.PutAsJsonAsync("/api/ai/keys/ollama", new { apiKey = NewKey("sk-") }, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        _factory.Validator.Calls.Count.ShouldBe(calls);
    }

    /// <summary>Test key'i (rastgele; gerçek değil).</summary>
    /// <param name="prefix">Önek.</param>
    /// <returns>Key.</returns>
    internal static string NewKey(string prefix) => prefix + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")[..6];

    /// <summary>Metinlerde key'in ne kendisi ne de 8+ karakterlik bir parçası geçer.</summary>
    /// <param name="key">Key.</param>
    /// <param name="texts">Metinler.</param>
    internal static void ShouldNotLeak(string key, params string[] texts)
    {
        foreach (var text in texts)
        {
            for (var i = 0; i + 8 <= key.Length; i++)
            {
                text.ShouldNotContain(key.Substring(i, 8), Case.Sensitive, $"parça {i}");
            }
        }
    }

    private async Task<(HttpClient Client, Guid UserId)> NewUserAsync(CancellationToken ct)
    {
        var email = AuthenticatedClient.NewEmail();
        var client = await _factory.CreateUserClientAsync(ct, email);
        return (client, _factory.Users.Get(email).Id);
    }

    private static string FieldError(string problemJson)
    {
        var errors = JsonDocument.Parse(problemJson).RootElement.GetProperty("errors");
        return errors.EnumerateObject().Single(p => string.Equals(p.Name, "ApiKey", StringComparison.OrdinalIgnoreCase)).Value[0].GetString()!;
    }

    private static async Task<JsonElement> Row(HttpClient client, string provider, CancellationToken ct)
    {
        var list = await client.GetFromJsonAsync<JsonElement>("/api/ai/keys", ct);
        return list.EnumerateArray().Single(r => r.GetProperty("providerId").GetString() == provider).Clone();
    }
}
