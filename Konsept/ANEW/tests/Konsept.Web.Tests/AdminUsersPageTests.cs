using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Admin Kullanıcılar ve ban, Web tarafı (spec 0024 AC-1, AC-2, AC-8, AC-12, AC-14): sekme İstatistikler'den sonra, banlı kullanıcı
/// kartı, onay pencereleri ve etiketli denetimler; Ajax uçları Api'ye parametreleri ve sebebi iletir (antiforgery ile); yetkisiz
/// kullanıcı 403 ve Api'ye gidilmez; banlı girişte "Hesabınız askıya alınmıştır.".
/// </summary>
public sealed class AdminUsersPageTests
{
    /// <summary>
    /// Panel: sekmeler İstatistikler · Kullanıcılar · Aktiviteler; Kullanıcılar paneli gizli açılır; banlı kullanıcı kartı; durum filtresi
    /// Tümü/Aktif/Banlı (varsayılan Tümü); ban penceresinde etiketli sebep alanı, sayaç ve uyarı; denetimler düğme/etiketli, durumlar
    /// metinle duyurulur (AC-2, AC-12, AC-14).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminPanel_UsersTab_MarkupAccessible()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateAdminClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/admin", ct);

        page.QuerySelectorAll("#admin-panel [role='tab']").Select(t => t.TextContent.Trim()).ShouldBe(["İstatistikler", "Kullanıcılar", "Aktiviteler", "Hata bildirimleri"]); // plan 0025 A-1.
        page.QuerySelector("#admin-users")!.HasAttribute("hidden").ShouldBeTrue();
        page.QuerySelector("#admin-users")!.GetAttribute("data-users-url").ShouldBe("/admin/kullanicilar");
        page.QuerySelectorAll(".admin-card dt").Select(d => d.TextContent).ShouldContain("Banlı kullanıcı");
        page.QuerySelectorAll("#admin-user-status option").Select(o => o.TextContent).ShouldBe(["Tümü", "Aktif", "Banlı"]);
        page.QuerySelector("#admin-user-status option[selected]")!.GetAttribute("value").ShouldBe("all");
        foreach (var id in new[] { "admin-user-search", "admin-user-status", "admin-ban-reason" })
        {
            page.QuerySelector($"label[for='{id}']").ShouldNotBeNull(id);
        }

        page.QuerySelectorAll("#admin-user-table th[scope='col']").Select(t => t.TextContent)
            .ShouldBe(["Ad", "E-posta", "Kayıt", "Son giriş", "Durum", "Rol", "Klip", "İşlem"]);
        page.QuerySelector("#admin-users-status")!.GetAttribute("role").ShouldBe("status");
        page.QuerySelector("#admin-users-error")!.GetAttribute("role").ShouldBe("alert");
        page.QuerySelector("#admin-ban-dialog [role='dialog']")!.GetAttribute("aria-modal").ShouldBe("true");
        page.QuerySelector("#admin-ban-reason-error")!.TextContent.ShouldBe("En fazla 500 karakter");
        page.QuerySelector("#admin-ban-ok")!.TagName.ShouldBe("BUTTON");
        page.QuerySelector("#admin-unban-ok")!.TextContent.ShouldBe("Banı kaldır");
        page.QuerySelector("#admin-users")!.GetAttribute("data-text-cannot-self").ShouldBe("Kendinizi banlayamazsınız");
        page.QuerySelector("#admin-users")!.GetAttribute("data-text-cannot-admin").ShouldBe("Admin hesapları banlanamaz");
        page.QuerySelector("#admin-users")!.GetAttribute("data-text-no-match").ShouldBe("Eşleşen kullanıcı yok");
        var css = File.ReadAllText(Path.Combine(WebPaths.WebProject, "wwwroot", "css", "site.css"));
        css.ReplaceLineEndings("\n").ShouldContain(".admin-status--banned {\n  font-weight: 600;\n  color: var(--color-error);");
    }

    /// <summary>
    /// Ajax uçları: liste sayfa/arama/durumu, ban sebebi ve ban kaldırmayı Api'ye iletir; yanıtı geçirir (AC-2, AC-4).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminUsersAjax_ForwardsToApi()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var id = Guid.NewGuid();
        factory.Api.GetAdminUsersAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(JsonDocument.Parse("""{"total":1}""").RootElement));
        factory.Api.SetUserBanAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(JsonDocument.Parse("""{"status":"banned"}""").RootElement));
        using var client = await factory.CreateAdminClientAsync(ct);

        using var list = await GetAjaxAsync(client, "/admin/kullanicilar?sayfa=2&ara=ay%C5%9Fe&durum=banned", ct);
        using var ban = await PostAjaxAsync(client, $"/admin/kullanicilar/{id}/banla", new { reason = "spam" }, ct);
        using var unban = await PostAjaxAsync(client, $"/admin/kullanicilar/{id}/ban-kaldir", new { }, ct);

        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await list.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("total").GetInt32().ShouldBe(1);
        ban.StatusCode.ShouldBe(HttpStatusCode.OK);
        unban.StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).GetAdminUsersAsync(2, "ayşe", "banned", Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SetUserBanAsync(
            id, true, Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("reason").GetString() == "spam"), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SetUserBanAsync(id, false, null, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Yetkisi olmayan kullanıcı: liste, ban ve ban kaldırma Ajax'ı 403 "Bu işlem için yetkiniz yok"; Api'ye istek gitmez (AC-1).
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminUsersAjax_UserRole_Forbidden_NoApiCall()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var user = await factory.CreateSignedInClientAsync(ct);
        var id = Guid.NewGuid();

        using var list = await GetAjaxAsync(user, "/admin/kullanicilar?sayfa=1", ct);
        using var ban = await PostAjaxAsync(user, $"/admin/kullanicilar/{id}/banla", new { reason = "x" }, ct);
        using var unban = await PostAjaxAsync(user, $"/admin/kullanicilar/{id}/ban-kaldir", new { }, ct);

        foreach (var response in new[] { list, ban, unban })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("message").GetString().ShouldBe("Bu işlem için yetkiniz yok");
        }

        await factory.Api.DidNotReceive().GetAdminUsersAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().SetUserBanAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Banlı hesap doğru şifre: giriş ekranında "Hesabınız askıya alınmıştır." görünür, giriş olmaz (AC-8).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Login_Suspended_ShowsSuspendedMessage()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);
        factory.Api.LoginAsync(Arg.Any<LoginApiRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthSessionResponse>.Failure(new Error(ApiClientErrors.SuspendedCode, "x", ErrorType.Unauthorized)));

        using var response = await SignedInClient.PostFormAsync(
            client, "/giris", new Dictionary<string, string>(StringComparer.Ordinal) { ["Email"] = "a@x.com", ["Password"] = "Sifre1234" }, ct);
        var page = await WebFactory.ParseAsync(await response.Content.ReadAsStringAsync(ct), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.GetElementById("form-message")!.TextContent.ShouldBe("Hesabınız askıya alınmıştır.");
        response.Headers.Contains("Set-Cookie").ShouldBeFalse();
    }

    private static Task<HttpResponseMessage> GetAjaxAsync(HttpClient client, string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Accept.ParseAdd("application/json");
        return client.SendAsync(request, ct);
    }

    private static async Task<HttpResponseMessage> PostAjaxAsync(HttpClient client, string path, object body, CancellationToken ct)
    {
        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var token = page.QuerySelector("meta[name='csrf-token']")!.GetAttribute("content")!;
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("RequestVerificationToken", token);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Accept.ParseAdd("application/json");
        return await client.SendAsync(request, ct);
    }
}
