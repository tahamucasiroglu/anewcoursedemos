using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Profiller, Api tarafı (spec 0003 AC-1, AC-3, AC-5, AC-7–AC-9, AC-11, AC-12, AC-14, AC-15; H-1 sahiplik). Bellek deposu; gerçek
/// PostgreSQL ve kasıtlı bozma <see cref="ProfilesIntegrationTests"/>'te.
/// </summary>
public sealed class ProfileApiTests : IClassFixture<ProfilesApiFactory>
{
    private readonly ProfilesApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ProfileApiTests"/> class.</summary>
    /// <param name="factory">Profiles test fabrikası.</param>
    public ProfileApiTests(ProfilesApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Geçerli bir profil gövdesi.</summary>
    /// <param name="name">Kanal adı.</param>
    /// <param name="content">Kanal içeriği.</param>
    /// <param name="seconds">Hedef süre.</param>
    /// <param name="language">Video dili.</param>
    /// <param name="format">Biçim.</param>
    /// <returns>Gövde.</returns>
    public static object Body(string name, string content = "Bizans tarihi kanalı.", int? seconds = 90, string? language = "tr", string? format = "landscape") =>
        new { channelName = name, language, format, durationSeconds = seconds, content };

    /// <summary>AC-1: geçerli profil kaydedilir, kendiliğinden seçili olur ve sonraki listede (yenileme) durur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Create_Valid_SelectedAndPersisted()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        await CreateAsync(client, "Birinci", ct);

        using var response = await client.PostAsJsonAsync("/api/profiles", Body("Teknoloji", seconds: 75, language: "en", format: "portrait"), ct);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var list = await ListAsync(client, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        created.GetProperty("channelName").GetString().ShouldBe("Teknoloji");
        created.GetProperty("language").GetString().ShouldBe("en");
        created.GetProperty("format").GetString().ShouldBe("portrait");
        created.GetProperty("durationSeconds").GetInt32().ShouldBe(75);
        list.GetProperty("selectedId").GetGuid().ShouldBe(created.GetProperty("id").GetGuid());
        Names(list).ShouldBe(["Birinci", "Teknoloji"]);
    }

    /// <summary>AC-3: beş onaylı şablon sırasıyla gelir; her biri Türkçe, geçerli alanlı ve 2–4 cümlelik içeriklidir; şablon profil oluşturmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Templates_FiveApproved()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);

        var list = await ListAsync(client, ct);
        var templates = list.GetProperty("templates").EnumerateArray().ToList();

        templates.Select(t => t.GetProperty("title").GetString()).ShouldBe(
            ["Tarih – Bizans", "Tarih – Osmanlı", "Teknoloji", "Antik teknoloji", "Günümüz teknolojisi"]);
        foreach (var t in templates)
        {
            t.GetProperty("language").GetString().ShouldBe("tr");
            new[] { "landscape", "portrait" }.ShouldContain(t.GetProperty("format").GetString());
            t.GetProperty("durationSeconds").GetInt32().ShouldBeInRange(15, 900);
            t.GetProperty("channelName").GetString()!.Length.ShouldBeInRange(1, 80);
            var sentences = t.GetProperty("content").GetString()!.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            sentences.Length.ShouldBeInRange(2, 4);
        }

        templates[0].GetProperty("channelName").GetString().ShouldBe("Bizans'ın İzinde");
        list.GetProperty("items").GetArrayLength().ShouldBe(0);
        list.GetProperty("maxProfiles").GetInt32().ShouldBe(20);
    }

    /// <summary>AC-5: düzenleme yalnızca o profili değiştirir; aynı profil art arda iki kez kaydedilirse son değerler kalır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Update_OnlyThatProfile_LastWriteWins()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var a = await CreateAsync(client, "Alfa", ct);
        var b = await CreateAsync(client, "Beta", ct);
        var before = (await ListAsync(client, ct)).GetProperty("items").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == b).GetRawText();

        using var first = await client.PutAsJsonAsync($"/api/profiles/{a}", Body("Alfa 2", "Sekme bir."), ct);
        using var second = await client.PutAsJsonAsync($"/api/profiles/{a}", Body("Alfa 3", "Sekme iki.", seconds: 900, format: "portrait"), ct);
        var list = await ListAsync(client, ct);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var edited = list.GetProperty("items").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == a);
        edited.GetProperty("channelName").GetString().ShouldBe("Alfa 3");
        edited.GetProperty("content").GetString().ShouldBe("Sekme iki.");
        edited.GetProperty("durationSeconds").GetInt32().ShouldBe(900);
        list.GetProperty("items").EnumerateArray().Single(p => p.GetProperty("id").GetGuid() == b).GetRawText().ShouldBe(before);
    }

    /// <summary>AC-7: boş/yalnızca boşluk alanlar ve listede olmayan dil/biçim alan hatasıyla reddedilir; hiçbir şey kaydedilmez.</summary>
    /// <param name="field">Beklenen hatalı alan.</param>
    /// <param name="name">Kanal adı.</param>
    /// <param name="content">Kanal içeriği.</param>
    /// <param name="language">Video dili.</param>
    /// <param name="format">Biçim.</param>
    /// <param name="message">Beklenen mesaj.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData("ChannelName", "", "içerik", "tr", "landscape", "Kanal adını girin.")]
    [InlineData("ChannelName", "   ", "içerik", "tr", "landscape", "Kanal adını girin.")]
    [InlineData("Content", "Ad", " \t ", "tr", "landscape", "Kanal içeriğini girin.")]
    [InlineData("Language", "Ad", "içerik", "de", "landscape", "Listeden bir video dili seçin.")]
    [InlineData("Language", "Ad", "içerik", null, "landscape", "Listeden bir video dili seçin.")]
    [InlineData("Format", "Ad", "içerik", "tr", "square", "Yatay ya da dikey biçimi seçin.")]
    public async Task Create_Invalid_FieldErrors(string field, string name, string content, string? language, string format, string message)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);

        using var response = await client.PostAsJsonAsync("/api/profiles", Body(name, content, language: language, format: format), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        FieldError(await response.Content.ReadAsStringAsync(ct), field).ShouldBe(message);
        (await ListAsync(client, ct)).GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    /// <summary>
    /// AC-7: aynı kullanıcıda tr-TR büyük/küçük harf duyarsız aynı ad ("İstanbul" ile "istanbul") hem oluşturmada hem düzenlemede
    /// reddedilir; başka bir kullanıcı aynı adı kullanabilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Create_DuplicateNameTurkishCase_Rejected_OtherUserAllowed()
    {
        var ct = TestContext.Current.CancellationToken;
        using var a = await _factory.CreateUserClientAsync(ct);
        using var b = await _factory.CreateUserClientAsync(ct);
        await CreateAsync(a, "İstanbul", ct);
        var other = await CreateAsync(a, "Ankara", ct);

        using var duplicate = await a.PostAsJsonAsync("/api/profiles", Body("  istanbul "), ct);
        using var rename = await a.PutAsJsonAsync($"/api/profiles/{other}", Body("İSTANBUL"), ct);
        using var otherUser = await b.PostAsJsonAsync("/api/profiles", Body("istanbul"), ct);

        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        FieldError(await duplicate.Content.ReadAsStringAsync(ct), "ChannelName").ShouldBe("Bu kanal adını başka bir profilinizde kullanıyorsunuz.");
        rename.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        otherUser.StatusCode.ShouldBe(HttpStatusCode.OK);
        Names(await ListAsync(a, ct)).ShouldBe(["Ankara", "İstanbul"]);
    }

    /// <summary>AC-7: baştaki/sondaki boşluklar kırpılarak kaydedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Create_Trimmed()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);

        using var response = await client.PostAsJsonAsync("/api/profiles", Body("  \tKanal  ", "  içerik metni \n "), ct);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        created.GetProperty("channelName").GetString().ShouldBe("Kanal");
        created.GetProperty("content").GetString().ShouldBe("içerik metni");
    }

    /// <summary>
    /// AC-8: kanal adı 1 ve 80'de kabul, 81'de ret; içerik 1 ve 2000'de kabul, 2001'de ret (kırpılmış uzunluk); süre 15 ve 900'de
    /// kabul, 14 ve 901'de ret.
    /// </summary>
    /// <param name="nameLength">Ad uzunluğu.</param>
    /// <param name="contentLength">İçerik uzunluğu.</param>
    /// <param name="seconds">Süre.</param>
    /// <param name="rejectedField">Ret beklenen alan; <c>null</c> ise kabul.</param>
    /// <returns>Görev.</returns>
    [Theory]
    [InlineData(1, 1, 15, null)]
    [InlineData(80, 2000, 900, null)]
    [InlineData(81, 10, 60, "ChannelName")]
    [InlineData(10, 2001, 60, "Content")]
    [InlineData(10, 10, 14, "DurationSeconds")]
    [InlineData(10, 10, 901, "DurationSeconds")]
    public async Task Create_Boundaries(int nameLength, int contentLength, int seconds, string? rejectedField)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var name = "  " + new string('ş', nameLength) + "  ";
        var content = " " + new string('ı', contentLength) + "\n";

        using var response = await client.PostAsJsonAsync("/api/profiles", Body(name, content, seconds), ct);

        if (rejectedField is null)
        {
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
        else
        {
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            FieldError(await response.Content.ReadAsStringAsync(ct), rejectedField).ShouldNotBeNullOrWhiteSpace();
        }
    }

    /// <summary>AC-9: 20. profil kaydedilir; 21. profil isteği sunucuda "En fazla 20 profil" ile reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Create_Limit20_21stRejected()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        for (var i = 1; i <= 19; i++)
        {
            await CreateAsync(client, $"Kanal {i}", ct);
        }

        using var twentieth = await client.PostAsJsonAsync("/api/profiles", Body("Kanal 20"), ct);
        using var twentyFirst = await client.PostAsJsonAsync("/api/profiles", Body("Kanal 21"), ct);
        var problem = await twentyFirst.Content.ReadFromJsonAsync<JsonElement>(ct);

        twentieth.StatusCode.ShouldBe(HttpStatusCode.OK);
        twentyFirst.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        problem.GetProperty("title").GetString().ShouldBe("En fazla 20 profil oluşturabilirsiniz");
        problem.GetProperty("code").GetString().ShouldBe("profiles.limit");
        (await ListAsync(client, ct)).GetProperty("items").GetArrayLength().ShouldBe(20);
    }

    /// <summary>
    /// AC-11: liste tr-TR alfabetiktir ("Cem" &lt; "Çağ" &lt; "Dünya"); seçim sunucuda hatırlanır (başka istemci = başka tarayıcı, aynı
    /// seçim); hatırlanan profil silinince ilk oluşturulan profil seçili gelir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task List_TurkishOrder_SelectionRemembered_FallbackFirstCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await _factory.CreateUserClientAsync(ct, email);
        var dunya = await CreateAsync(client, "Dünya", ct);
        await CreateAsync(client, "Çağ", ct);
        var cem = await CreateAsync(client, "Cem", ct);

        using var select = await client.PutAsJsonAsync("/api/profiles/selected", new { profileId = cem }, ct);
        using var otherBrowser = _factory.CreateClient();
        var session = await AuthenticatedClient.LoginAsync(otherBrowser, _factory.Services, email, AuthenticatedClient.Password, ct);
        otherBrowser.DefaultRequestHeaders.Authorization = new("Bearer", session.GetProperty("accessToken").GetString());
        var seenElsewhere = await ListAsync(otherBrowser, ct);
        (await client.DeleteAsync(new Uri($"/api/profiles/{cem}", UriKind.Relative), ct)).EnsureSuccessStatusCode();
        var afterDelete = await ListAsync(client, ct);

        select.StatusCode.ShouldBe(HttpStatusCode.OK);
        Names(seenElsewhere).ShouldBe(["Cem", "Çağ", "Dünya"]);
        seenElsewhere.GetProperty("selectedId").GetGuid().ShouldBe(cem);
        afterDelete.GetProperty("selectedId").GetGuid().ShouldBe(dunya);
    }

    /// <summary>AC-12: profili olmayan kullanıcıya otomatik profil oluşturulmaz; seçili profil yoktur.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task List_NoProfiles_EmptyNoAutoCreate()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);

        var first = await ListAsync(client, ct);
        var second = await ListAsync(client, ct);

        first.GetProperty("items").GetArrayLength().ShouldBe(0);
        second.GetProperty("items").GetArrayLength().ShouldBe(0);
        second.GetProperty("selectedId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>
    /// AC-14: silme kalıcıdır (yeniden listede yok), yanıt yeni listeyi ve ilk oluşturulan kalan profili seçili döndürür; son profil
    /// silinince liste boş, seçili profil yoktur; olmayan profil 404.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Delete_RemovesAndSelectsFirstCreated()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateUserClientAsync(ct);
        var first = await CreateAsync(client, "Zeta", ct);
        var second = await CreateAsync(client, "Alfa", ct);

        using var deleted = await client.DeleteAsync(new Uri($"/api/profiles/{second}", UriKind.Relative), ct);
        var body = await deleted.Content.ReadFromJsonAsync<JsonElement>(ct);
        using var again = await client.DeleteAsync(new Uri($"/api/profiles/{second}", UriKind.Relative), ct);
        using var last = await client.DeleteAsync(new Uri($"/api/profiles/{first}", UriKind.Relative), ct);
        var empty = await last.Content.ReadFromJsonAsync<JsonElement>(ct);

        deleted.StatusCode.ShouldBe(HttpStatusCode.OK);
        Names(body).ShouldBe(["Zeta"]);
        body.GetProperty("selectedId").GetGuid().ShouldBe(first);
        again.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        empty.GetProperty("items").GetArrayLength().ShouldBe(0);
        empty.GetProperty("selectedId").ValueKind.ShouldBe(JsonValueKind.Null);
        _factory.Profiles.Rows.ShouldNotContain(p => p.Id == first || p.Id == second);
    }

    /// <summary>
    /// AC-15 (H-1): B, A'nın profilini listede görmez; kimliğini bilse bile düzenleyemez, silemez, seçemez — her biri A'nın
    /// olmayan bir profiliyle aynı "Profil bulunamadı" 404'üdür (varlık açığa çıkmaz) ve A'nın profili değişmez.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Ownership_OtherUser_NotFound_NoLeak()
    {
        var ct = TestContext.Current.CancellationToken;
        using var a = await _factory.CreateUserClientAsync(ct);
        using var b = await _factory.CreateUserClientAsync(ct);
        var secret = await CreateAsync(a, "Gizli Kanal", ct);
        var before = _factory.Profiles.Rows.Single(p => p.Id == secret);
        var missing = Guid.CreateVersion7();

        var listB = await ListAsync(b, ct);
        var responses = new List<(HttpResponseMessage Theirs, HttpResponseMessage Missing)>
        {
            (await b.PutAsJsonAsync($"/api/profiles/{secret}", Body("Ele geçir"), ct), await b.PutAsJsonAsync($"/api/profiles/{missing}", Body("Ele geçir"), ct)),
            (await b.DeleteAsync(new Uri($"/api/profiles/{secret}", UriKind.Relative), ct), await b.DeleteAsync(new Uri($"/api/profiles/{missing}", UriKind.Relative), ct)),
            (await b.PutAsJsonAsync("/api/profiles/selected", new { profileId = secret }, ct), await b.PutAsJsonAsync("/api/profiles/selected", new { profileId = missing }, ct)),
        };

        listB.GetRawText().ShouldNotContain(secret.ToString());
        listB.GetRawText().ShouldNotContain("Gizli Kanal");
        foreach (var (theirs, notThere) in responses)
        {
            theirs.StatusCode.ShouldBe(HttpStatusCode.NotFound);
            var theirsBody = await theirs.Content.ReadFromJsonAsync<JsonElement>(ct);
            var missingBody = await notThere.Content.ReadFromJsonAsync<JsonElement>(ct);
            theirsBody.GetProperty("title").GetString().ShouldBe("Profil bulunamadı.");
            theirsBody.GetProperty("title").GetString().ShouldBe(missingBody.GetProperty("title").GetString());
            theirs.Dispose();
            notThere.Dispose();
        }

        _factory.Profiles.Rows.Single(p => p.Id == secret).ShouldBeEquivalentTo(before);
        (await b.GetFromJsonAsync<JsonElement>("/api/profiles", ct)).GetProperty("selectedId").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>AC-15: girişsiz istek profil uçlarında 401 alır; hiçbir profil oluşmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Anonymous_ProfileEndpoints_401()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = _factory.CreateClient();
        var id = Guid.CreateVersion7();
        var count = _factory.Profiles.Rows.Count;

        using var list = await client.GetAsync(new Uri("/api/profiles", UriKind.Relative), ct);
        using var create = await client.PostAsJsonAsync("/api/profiles", Body("Anonim"), ct);
        using var update = await client.PutAsJsonAsync($"/api/profiles/{id}", Body("Anonim"), ct);
        using var delete = await client.DeleteAsync(new Uri($"/api/profiles/{id}", UriKind.Relative), ct);
        using var select = await client.PutAsJsonAsync("/api/profiles/selected", new { profileId = id }, ct);

        foreach (var response in new[] { list, create, update, delete, select })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        _factory.Profiles.Rows.Count.ShouldBe(count);
    }

    /// <summary>Profil oluşturur ve kimliğini döndürür.</summary>
    /// <param name="client">İstemci.</param>
    /// <param name="name">Kanal adı.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Profil kimliği.</returns>
    public static async Task<Guid> CreateAsync(HttpClient client, string name, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(client);
        using var response = await client.PostAsJsonAsync("/api/profiles", Body(name), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ListAsync(HttpClient client, CancellationToken ct) =>
        await client.GetFromJsonAsync<JsonElement>("/api/profiles", ct);

    private static List<string?> Names(JsonElement list) =>
        [.. list.GetProperty("items").EnumerateArray().Select(p => p.GetProperty("channelName").GetString())];

    private static string FieldError(string problemJson, string field)
    {
        var errors = JsonDocument.Parse(problemJson).RootElement.GetProperty("errors");
        return errors.EnumerateObject().Single(p => string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase)).Value[0].GetString()!;
    }
}
