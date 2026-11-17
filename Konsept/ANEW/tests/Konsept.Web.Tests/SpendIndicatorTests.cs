using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Infrastructure;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Üst bardaki aylık harcama ve Ayarlar'daki deneme çağrısı, Web tarafı (spec 0013 AC-14, AC-16, AC-18; plan 0013 Risk 3):
/// gösterge girişli kullanıcıda logo + adın hemen yanında, tıklanabilir bir kontrol değil (odak almaz), erişilebilir adı ve
/// biçimi ("$2,37", yarım yukarı) sunucudan; giriş yoksa hiç yok; Api hatasında Türkçe hata; deneme isteği CSRF korumalı ve Api'ye
/// iletilir.
/// </summary>
public sealed class SpendIndicatorTests
{
    /// <summary>AC-14: gösterim iki hane, yarım yukarı, Türkçe virgül; toplama yuvarlanmamış değerle yapılmıştır.</summary>
    [Fact]
    public void SpendDisplay_HalfUp_TurkishComma()
    {
        SpendDisplay.Amount(1.234567m + 1.131m).ShouldBe("2,37");
        SpendDisplay.Amount(2.365m).ShouldBe("2,37");
        SpendDisplay.Amount(2.364999m).ShouldBe("2,36");
        SpendDisplay.Amount(0m).ShouldBe("0,00");
        SpendDisplay.Amount(0.005m).ShouldBe("0,01");
        SpendDisplay.Amount(0.004999m).ShouldBe("0,00");
        SpendDisplay.Amount(1234.5m).ShouldBe("1234,50");
    }

    /// <summary>AC-18: girişli kullanıcıda gösterge logonun yanında, pasif değil, odak almaz, yükleniyor durumunda başlar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SpendIndicator_SignedIn_NotFocusable_AccessibleName()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        factory.Api.GetMonthlySpendAsync(Arg.Any<CancellationToken>())
            .Returns(Result<MonthlySpendResponse>.Success(new MonthlySpendResponse(2.365567m, DateTimeOffset.UtcNow)));

        var doc = await WebFactory.GetDocumentAsync(client, "/", ct);
        var spend = doc.GetElementById("monthly-spend")!;
        using var response = await AjaxGetAsync(client, "/harcama", ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        spend.PreviousElementSibling!.ClassList.ShouldContain("topbar__brand");
        spend.ClassList.ShouldNotContain("passive");
        spend.HasAttribute("tabindex").ShouldBeFalse();
        spend.LocalName.ShouldBe("span");
        spend.GetAttribute("role").ShouldBe("status");
        spend.GetAttribute("aria-live").ShouldBe("off");
        spend.GetAttribute("data-state").ShouldBe("loading");
        spend.TextContent.ShouldBe("Bu ay: …");
        spend.GetAttribute("data-text-failed").ShouldBe("Bu ay: —");
        spend.GetAttribute("data-text-failed-hint").ShouldBe("Harcama alınamadı");
        body.GetProperty("text").GetString().ShouldBe("Bu ay: $2,37");
        body.GetProperty("label").GetString().ShouldBe("Bu ayki harcama: 2,37 dolar");
    }

    /// <summary>AC-18: giriş yapmamış kullanıcıya gösterge hiç görünmez; uç 401 döner ve Api çağrılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SpendIndicator_Anonymous_Hidden()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        var login = await WebFactory.GetDocumentAsync(client, "/giris", ct);
        using var response = await AjaxGetAsync(client, "/harcama", ct);

        login.GetElementById("monthly-spend").ShouldBeNull();
        login.QuerySelector(".topbar__spend").ShouldBeNull();
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await factory.Api.DidNotReceive().GetMonthlySpendAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>AC-16 (sunucu tarafı): Api'ye ulaşılamazsa uç hata döner (gösterge "—" gösterir); sayfanın kendisi açılır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SpendIndicator_ApiFailure_ErrorJson_PageStillRenders()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        factory.Api.GetMonthlySpendAsync(Arg.Any<CancellationToken>())
            .Returns(Result<MonthlySpendResponse>.Failure(new Error("api.unavailable", "API erişilemiyor", ErrorType.Unavailable)));

        using var response = await AjaxGetAsync(client, "/harcama", ct);
        var doc = await WebFactory.GetDocumentAsync(client, "/", ct);

        response.IsSuccessStatusCode.ShouldBeFalse();
        doc.GetElementById("monthly-spend").ShouldNotBeNull();
        doc.QuerySelector("h1.page__title").ShouldNotBeNull();
    }

    /// <summary>Deneme çağrısı: CSRF'siz istek reddedilir ve Api çağrılmaz; CSRF'li istek modeli Api'ye iletir, Api'nin Türkçe mesajı döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trial_ForwardsToApi_CsrfProtected()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        factory.Api.TrialCallAsync("anthropic/claude-sonnet-5-5", Arg.Any<CancellationToken>())
            .Returns(Result<TrialCallResponse>.Failure(new Error("ai.no_permission", "Bu modeli kullanma yetkiniz yok", ErrorType.Validation)));

        using var withoutToken = await client.PostAsJsonAsync("/ayarlar/modeller/dene", new { modelId = "anthropic/claude-sonnet-5-5" }, ct);
        withoutToken.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().TrialCallAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>());

        var page = await WebFactory.GetDocumentAsync(client, "/ayarlar", ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/ayarlar/modeller/dene") { Content = JsonContent.Create(new { modelId = "anthropic/claude-sonnet-5-5" }) };
        request.Headers.Add("RequestVerificationToken", page.QuerySelector("meta[name='csrf-token']")!.GetAttribute("content")!);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        using var response = await client.SendAsync(request, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("message").GetString().ShouldBe("Bu modeli kullanma yetkiniz yok");
        await factory.Api.Received(1).TrialCallAsync("anthropic/claude-sonnet-5-5", Arg.Any<CancellationToken>());
        page.GetElementById("settings")!.GetAttribute("data-text-no-permission").ShouldBe("Yetki yok");
    }

    private static async Task<HttpResponseMessage> AjaxGetAsync(HttpClient client, string path, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        return await client.SendAsync(request, ct);
    }
}
