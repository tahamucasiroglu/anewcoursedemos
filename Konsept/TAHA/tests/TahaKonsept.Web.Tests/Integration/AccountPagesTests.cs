using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using NSubstitute;
using Shouldly;
using TahaKonsept.Web.Tests.Support;
using Xunit;

namespace TahaKonsept.Web.Tests.Integration;

/// <summary>
/// Giriş / kayıt sayfaları uçtan uca (bellek içi Web host, sahte oturum servisi): kayıt sayfası anonim açılır ve form gösterir;
/// kayıt kapalıyken form yerine mesaj; giriş ↔ kayıt bağlantıları; eski <c>/Kurulum</c> kayıt sayfasına yönlenir; oturumsuz <c>/</c> girişe gider;
/// oturum açıkken giriş/kayıt sayfası ana sayfaya döner.
/// </summary>
public sealed class AccountPagesTests
{
    /// <summary>Kayıt açıkken form ve "Giriş yap" bağlantısı görünür.</summary>
    [Fact]
    public async Task Register_page_shows_form_when_open()
    {
        await using var factory = new WebFactory();
        factory.Account.IsRegistrationOpenAsync(Arg.Any<CancellationToken>()).Returns(true);
        using var client = Anonymous(factory);

        var html = await GetOk(client, "/Account/Register");

        html.ShouldContain("id=\"tk-register-form\"");
        html.ShouldContain("name=\"passwordConfirm\"");
        html.ShouldContain("Zaten hesabın var mı?");
        html.ShouldContain("href=\"/Account/Login\"");
        html.ShouldNotContain("tk-register-closed");
    }

    /// <summary>Kayıt kapalıyken form yerine açık bir mesaj görünür.</summary>
    [Fact]
    public async Task Register_page_shows_closed_message_when_closed()
    {
        await using var factory = new WebFactory();
        factory.Account.IsRegistrationOpenAsync(Arg.Any<CancellationToken>()).Returns(false);
        using var client = Anonymous(factory);

        var html = await GetOk(client, "/Account/Register");

        html.ShouldContain("id=\"tk-register-closed\"");
        html.ShouldContain("Yeni kayıtlar şu an kapalı");
        html.ShouldNotContain("id=\"tk-register-form\"");
    }

    /// <summary>Giriş sayfası e-posta da kabul ettiğini söyler ve kayıt bağlantısı verir.</summary>
    [Fact]
    public async Task Login_page_links_to_register_and_accepts_email()
    {
        await using var factory = new WebFactory();
        using var client = Anonymous(factory);

        var html = await GetOk(client, "/Account/Login");

        html.ShouldContain("Kullanıcı adı veya e-posta");
        html.ShouldContain("Hesabın yok mu?");
        html.ShouldContain("href=\"/Account/Register\"");
    }

    /// <summary>Eski kurulum adresi kayıt sayfasına yönlenir; oturumsuz ana sayfa girişe gider.</summary>
    /// <param name="path">İstenen adres.</param>
    /// <param name="expected">Beklenen yönlendirme parçası.</param>
    [Theory]
    [InlineData("/Kurulum", "/Account/Register")]
    [InlineData("/", "/Account/Login")]
    public async Task Legacy_and_root_routes_redirect(string path, string expected)
    {
        await using var factory = new WebFactory();
        using var client = Anonymous(factory);

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain(expected);
    }

    /// <summary>Oturum açık kullanıcı giriş/kayıt sayfasında kalmaz.</summary>
    /// <param name="path">Sayfa.</param>
    [Theory]
    [InlineData("/Account/Login")]
    [InlineData("/Account/Register")]
    public async Task Signed_in_user_is_sent_home(string path)
    {
        await using var factory = new WebFactory();
        using var client = factory.CreateUserClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldBe("/");
    }

    private static HttpClient Anonymous(WebFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<string> GetOk(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }
}
