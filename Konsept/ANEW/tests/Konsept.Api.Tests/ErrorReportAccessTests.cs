using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Support;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Hata bildirimi erişimi (spec 0022 AC-14, H-1): kimliksiz gönderim/önizleme/okuma reddedilir; normal kullanıcı kendi bildirimini de
/// başkasınınkini de okuyamaz (403); yalnızca <c>admin.panel</c> okur.
/// </summary>
public sealed class ErrorReportAccessTests : IClassFixture<IdentityApiFactory>
{
    private const string Path = "/api/error-reports";
    private readonly IdentityApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ErrorReportAccessTests"/> class.</summary>
    /// <param name="factory">Kimlik testleri API fabrikası (ilk admin ayarlı).</param>
    public ErrorReportAccessTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>Normal kullanıcı kendi ve başkasının bildirimini okuyamaz (403); admin okur (kaynak web, durum new).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ErrorReport_Read_AdminOnly()
    {
        var ct = TestContext.Current.CancellationToken;
        using var owner = await AuthenticatedClient.CreateAsync(_factory, ct);
        using var other = await AuthenticatedClient.CreateAsync(_factory, ct);
        using var admin = await _factory.CreateAdminClientAsync(ct);
        using var sent = await owner.PostAsJsonAsync(Path, new { description = "okuma yetkisi testi", page = "/" }, ct);
        var number = (await sent.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("number").GetInt64();

        using var ownRead = await owner.GetAsync(new Uri($"{Path}/{number}", UriKind.Relative), ct);
        using var otherRead = await other.GetAsync(new Uri($"{Path}/{number}", UriKind.Relative), ct);
        using var adminRead = await admin.GetAsync(new Uri($"{Path}/{number}", UriKind.Relative), ct);
        var body = await adminRead.Content.ReadFromJsonAsync<JsonElement>(ct);

        sent.StatusCode.ShouldBe(HttpStatusCode.OK);
        ownRead.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        otherRead.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await ownRead.Content.ReadAsStringAsync(ct)).ShouldNotContain("okuma yetkisi testi");
        adminRead.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("source").GetString().ShouldBe("web");
        body.GetProperty("status").GetString().ShouldBe("new");
        body.GetProperty("description").GetString().ShouldBe("okuma yetkisi testi");
    }

    /// <summary>Giriş yapılmamış ya da çıkış yapmış kullanıcının gönderimi, önizlemesi ve okuması 401'dir; kayıt oluşmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ErrorReport_Anonymous_Rejected()
    {
        var ct = TestContext.Current.CancellationToken;
        using var anonymous = _factory.CreateClient();
        using var signedOut = await AuthenticatedClient.CreateAsync(_factory, ct);
        using var logout = await signedOut.PostAsync(new Uri("/api/auth/logout", UriKind.Relative), null, ct);
        var before = _factory.ErrorReports.Reports.Count;

        using var post = await anonymous.PostAsJsonAsync(Path, new { description = "kimliksiz bildirim", page = "/" }, ct);
        using var afterLogout = await signedOut.PostAsJsonAsync(Path, new { description = "çıkıştan sonra bildirim", page = "/" }, ct);
        using var preview = await anonymous.GetAsync(new Uri(Path + "/preview", UriKind.Relative), ct);
        using var read = await anonymous.GetAsync(new Uri(Path + "/1", UriKind.Relative), ct);

        logout.IsSuccessStatusCode.ShouldBeTrue();
        post.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        afterLogout.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        preview.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        read.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        _factory.ErrorReports.Reports.Count.ShouldBe(before);
    }
}
