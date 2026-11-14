using System.Net;
using NSubstitute;
using Shouldly;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Tests.Support;
using Xunit;

namespace TahaKonsept.Web.Tests.Integration;

/// <summary>
/// Başlangıç sayfası ve Ayarlar indirme düğmesi uçtan uca (bellek içi Web host): giriş zorunluluğu, sürüm + boyutlu
/// birincil indirme düğmesi, dosya yoksa uyarı (bozuk bağlantı yok), <c>/Baslangic/Durum</c> JSON'u, menüdeki "Başlangıç" öğesi.
/// </summary>
public sealed class BaslangicPageTests : IClassFixture<WebFactory>
{
    private readonly WebFactory _factory;

    /// <summary>Fabrikayı alır.</summary>
    /// <param name="factory">Web fabrikası.</param>
    public BaslangicPageTests(WebFactory factory) => _factory = factory;

    /// <summary>Oturumsuz ziyaretçi giriş sayfasına, dönüş adresi Başlangıç olarak yönlendirilir.</summary>
    [Fact]
    public async Task Anonymous_visitor_is_redirected_to_login()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync(new Uri("/Baslangic", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().ShouldContain("/Account/Login?returnUrl=%2FBaslangic");
    }

    /// <summary>Sayfa sürüm ve boyutu sunucudaki bilgiden gösterir; menüde Başlangıç öğesi vardır.</summary>
    [Fact]
    public async Task Page_shows_primary_download_with_version_and_size()
    {
        _factory.Releases.GetCurrent().Returns(WebFactory.Published("7.8.9"));
        using var client = _factory.CreateUserClient();

        var html = await client.GetStringAsync(new Uri("/Baslangic", UriKind.Relative), TestContext.Current.CancellationToken);

        html.ShouldContain("Hoş geldiniz, taha!");
        html.ShouldContain("tk-btn tk-btn-primary tk-btn-lg");
        html.ShouldContain("/downloads/TahaKonsept.Agent-win-Setup.exe?v=7.8.9");
        html.ShouldContain("Sürüm 7.8.9");
        html.ShouldContain("86,9 MB");
        html.ShouldContain("id=\"bs-agent-form\"");
        html.ShouldContain("id=\"tk-nav-start\"");
        html.ShouldNotContain("data-agent-download-missing");
    }

    /// <summary>Kurulum dosyası yoksa bağlantı yerine açık bir uyarı çıkar (Başlangıç ve Ayarlar).</summary>
    [Fact]
    public async Task Missing_installer_shows_warning_instead_of_link()
    {
        _factory.Releases.GetCurrent().Returns(new AgentReleaseInfo { IsAvailable = false, FileName = "TahaKonsept.Agent-win-Setup.exe" });
        using var client = _factory.CreateUserClient();

        foreach (var page in new[] { "/Baslangic", "/Ayarlar" })
        {
            var html = await client.GetStringAsync(new Uri(page, UriKind.Relative), TestContext.Current.CancellationToken);
            html.ShouldContain("data-agent-download-missing");
            html.ShouldNotContain("data-agent-download ");
        }
    }

    /// <summary>Ayarlar › Masaüstü programı kartında birincil indirme düğmesi var, eski "yayınlanmadıysa" ipucu yok.</summary>
    [Fact]
    public async Task Settings_card_has_primary_download_and_no_stale_hint()
    {
        _factory.Releases.GetCurrent().Returns(WebFactory.Published("1.2.3"));
        using var client = _factory.CreateUserClient();

        var html = await client.GetStringAsync(new Uri("/Ayarlar", UriKind.Relative), TestContext.Current.CancellationToken);

        html.ShouldContain("Sürüm 1.2.3");
        html.ShouldContain("tk-btn tk-btn-primary tk-btn-lg");
        html.ShouldContain("id=\"st-agent-form\"");
        html.ShouldNotContain("henüz yayınlanmadıysa");
    }

    /// <summary>Durum ucu adımları metin durumlarıyla döndürür ve <c>yenile</c> parametresini servise iletir.</summary>
    [Fact]
    public async Task Status_endpoint_returns_steps_with_text_states()
    {
        _factory.Onboarding.GetStatusAsync(true, Arg.Any<CancellationToken>()).Returns(new OnboardingStatus
        {
            Steps = [new OnboardingStep { Key = OnboardingStepKeys.LlmKey, State = OnboardingStepState.Done, Detail = "Anthropic (Claude) kayıtlı." }],
            Remaining = 0,
            IsComplete = false,
        });
        using var client = _factory.CreateUserClient();

        var json = await client.GetStringAsync(new Uri("/Baslangic/Durum?yenile=true", UriKind.Relative), TestContext.Current.CancellationToken);

        json.ShouldContain("\"key\":\"llm-key\"");
        json.ShouldContain("\"state\":\"Done\"");
        json.ShouldContain("\"isComplete\":false");
        await _factory.Onboarding.Received().GetStatusAsync(true, Arg.Any<CancellationToken>());
    }
}
