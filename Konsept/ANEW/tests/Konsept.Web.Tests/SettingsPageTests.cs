using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Ayarlar, Web tarafı (spec 0004 AC-9, AC-13, AC-15, AC-17): üst bardaki "Ayarlar" artık çalışır ve sayfayı açar; sayfa
/// "API key'ler" ve "Modeller" bölümlerini yükleniyor durumlarıyla getirir; Ajax uçları giriş ister ve CSRF korumalıdır; key
/// yalnızca Api'ye iletilir; Api hatasında Türkçe genel mesaj için korelasyon numarası döner ve gövdede girilen key yoktur.
/// </summary>
public sealed partial class SettingsPageTests
{
    /// <summary>AC-13: "Ayarlar" pasif değildir; klavyeyle odaklanan gerçek bir düğmedir ve GET formuyla <c>/ayarlar</c>'ı açar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task TopBar_Settings_NotPassive_OpensSettings()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var home = await WebFactory.GetDocumentAsync(client, "/", ct);
        var settings = home.QuerySelector(".topbar__actions > button[aria-label='Ayarlar']")!;

        settings.ClassList.ShouldNotContain("passive");
        settings.HasAttribute("aria-disabled").ShouldBeFalse();
        settings.HasAttribute("tabindex").ShouldBeFalse();
        settings.GetAttribute("type").ShouldBe("submit");
        var form = home.GetElementById(settings.GetAttribute("form")!)!;
        form.LocalName.ShouldBe("form");
        form.GetAttribute("method").ShouldBe("get");
        form.GetAttribute("action").ShouldBe("/ayarlar");

        var page = await WebFactory.GetDocumentAsync(client, "/ayarlar", ct);
        page.QuerySelector("h1.page__title")!.TextContent.ShouldBe("Ayarlar");
        page.QuerySelector(".topbar__actions > button[aria-label='Ayarlar']")!.GetAttribute("aria-current").ShouldBe("page");
    }

    /// <summary>
    /// AC-13/AC-14/AC-17: sayfada "API key'ler" ve "Modeller" bölümleri, yükleniyor durumları, tür/sağlayıcı filtreleri (etiketli),
    /// boş durum, rol=alert hata alanları, Req 11 onay metni ve klavye odaklı onay penceresi vardır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SettingsPage_RendersSectionsAndStates()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/ayarlar", ct);
        var root = page.GetElementById("settings")!;

        root.QuerySelectorAll("h2.settings__title").Select(h => h.TextContent).ShouldBe(["API key'ler", "Modeller"]);
        page.GetElementById("keys-loading")!.TextContent.ShouldBe("Yükleniyor…");
        page.GetElementById("models-loading")!.TextContent.ShouldBe("Yükleniyor…");
        page.QuerySelector("label[for='model-kind']")!.TextContent.ShouldBe("Tür");
        page.QuerySelector("label[for='model-provider']")!.TextContent.ShouldBe("Sağlayıcı");
        page.GetElementById("models-empty")!.TextContent.ShouldBe("Bu filtrede model yok");
        page.GetElementById("keys-error")!.GetAttribute("role").ShouldBe("alert");
        page.GetElementById("models-error")!.GetAttribute("role").ShouldBe("alert");
        root.GetAttribute("data-text-not-set").ShouldBe("Girilmemiş");
        root.GetAttribute("data-text-unreadable").ShouldBe("Key okunamıyor — lütfen yeniden girin");
        root.GetAttribute("data-text-input-label").ShouldBe("{0} API key");
        string.Format(CultureInfo.InvariantCulture, root.GetAttribute("data-text-delete-confirm")!, "Anthropic").ShouldBe(
            "Bu key silinecek; Anthropic modelleri kullanılamaz olacak. Sıradaki işler bu sağlayıcıyı kullanıyorsa duracak.");
        var dialog = page.QuerySelector("#delete-dialog [role='alertdialog']")!;
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        dialog.QuerySelectorAll("button").Select(b => b.TextContent).ShouldBe(["Vazgeç", "Sil"]);

        var css = File.ReadAllText(Path.Combine(WebPaths.WebProject, "wwwroot", "css", "site.css"));
        var themes = VariableBlock().Matches(css).Select(m => Variables(m.Groups["body"].Value)).ToList();
        themes.Count.ShouldBe(2);
        foreach (var (fore, back) in new[]
        {
            ("--color-error", "--color-surface"), ("--color-success", "--color-surface"), ("--color-text-muted", "--color-surface"),
            ("--color-on-primary", "--color-primary"), ("--color-tooltip-text", "--color-tooltip-bg"), ("--color-text", "--color-surface-alt"),
        })
        {
            foreach (var theme in themes)
            {
                Contrast(theme[fore], theme[back]).ShouldBeGreaterThanOrEqualTo(4.5, $"{fore} / {back}");
            }
        }
    }

    /// <summary>AC-9: giriş yapmamış kullanıcı Ayarlar'a gidemez (girişe yönlenir); Ajax uçları 401 döner ve Api çağrılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Settings_Anonymous_RedirectsOrUnauthorized()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        using var page = await client.GetAsync(new Uri("/ayarlar", UriKind.Relative), ct);
        page.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        page.Headers.Location!.PathAndQuery.ShouldBe("/giris?returnUrl=%2Fayarlar");

        foreach (var path in new[] { "/ayarlar/api-keyler", "/ayarlar/modeller" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var response = await client.SendAsync(request, ct);
            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, path);
        }

        await factory.Api.DidNotReceive().GetApiKeysAsync(Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().GetCatalogAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Kaydetme yalnızca CSRF belirteciyle çalışır; key Api'ye iletilir ve yanıtta yalnızca maskeli hal döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SaveKey_ForwardsToApi_ReturnsMaskedOnly()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        const string Key = "sk-ant-web-test-0004-abcdef";
        factory.Api.SaveApiKeyAsync("anthropic", Key, Arg.Any<CancellationToken>())
            .Returns(Result<ApiKeyStatusResponse>.Success(new ApiKeyStatusResponse("anthropic", "Anthropic", "saved", "sk-…def", "verified", DateTimeOffset.UtcNow)));

        using var withoutToken = await client.PostAsJsonAsync("/ayarlar/api-keyler/anthropic", new { apiKey = Key }, ct);
        withoutToken.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().SaveApiKeyAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

        using var response = await PostAjaxAsync(client, "/ayarlar/api-keyler/anthropic", new { apiKey = Key }, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonDocument.Parse(text).RootElement.GetProperty("hint").GetString().ShouldBe("sk-…def");
        text.ShouldNotContain(Key);
        await factory.Api.Received(1).SaveApiKeyAsync("anthropic", Key, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// AC-15: kaydetme/silme sunucu hatasıyla biterse Web korelasyon numarasını taşır (Api'ninki), gövdede girilen key yoktur; alan
    /// hatası (sağlayıcı reddi) alan adıyla döner.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task SaveKey_ApiFailure_GenericMessageWithCorrelation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        const string Key = "sk-ant-web-fail-0004-zyxwvu";
        var serverError = new Error("api.error", "Beklenmeyen bir hata oluştu.", ErrorType.Unavailable)
        {
            Metadata = new Dictionary<string, object> { ["correlationId"] = "corr-0004" },
        };
        factory.Api.SaveApiKeyAsync("openai", Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result<ApiKeyStatusResponse>.Failure(serverError));
        factory.Api.DeleteApiKeyAsync("openai", Arg.Any<CancellationToken>()).Returns(Result<ApiKeyStatusResponse>.Failure(serverError));
        factory.Api.SaveApiKeyAsync("kimi", Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Result<ApiKeyStatusResponse>.Failure(
            new Error("ai.key_rejected", "Sağlayıcı bu key'i reddetti", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]> { ["ApiKey"] = ["Sağlayıcı bu key'i reddetti"] },
            }));

        using var save = await PostAjaxAsync(client, "/ayarlar/api-keyler/openai", new { apiKey = Key }, ct);
        using var delete = await PostAjaxAsync(client, "/ayarlar/api-keyler/openai/sil", new { }, ct);
        using var rejected = await PostAjaxAsync(client, "/ayarlar/api-keyler/kimi", new { apiKey = Key }, ct);

        foreach (var response in new[] { save, delete })
        {
            ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(500);
            var text = await response.Content.ReadAsStringAsync(ct);
            JsonDocument.Parse(text).RootElement.GetProperty("correlationId").GetString().ShouldBe("corr-0004");
            text.ShouldNotContain(Key);
        }

        rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync(ct)).RootElement;
        body.GetProperty("errors").GetProperty("ApiKey")[0].GetString().ShouldBe("Sağlayıcı bu key'i reddetti");
        body.GetRawText().ShouldNotContain(Key);
    }

    private static async Task<HttpResponseMessage> PostAjaxAsync(HttpClient client, string path, object body, CancellationToken ct)
    {
        var page = await WebFactory.GetDocumentAsync(client, "/ayarlar", ct);
        var token = page.QuerySelector("meta[name='csrf-token']")!.GetAttribute("content")!;
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("RequestVerificationToken", token);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        return await client.SendAsync(request, ct);
    }

    private static Dictionary<string, string> Variables(string body) =>
        VariableDeclaration().Matches(body).ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal);

    private static double Contrast(string a, string b)
    {
        var (l1, l2) = (Luminance(a), Luminance(b));
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int offset)
        {
            var c = int.Parse(hex.AsSpan(1 + offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(0)) + (0.7152 * Channel(2)) + (0.0722 * Channel(4));
    }

    [GeneratedRegex(@":root(?:\[data-theme=""dark""\])?\s*\{(?<body>[^}]*)\}")]
    private static partial Regex VariableBlock();

    [GeneratedRegex(@"(?<name>--color-[a-z-]+)\s*:\s*(?<value>#[0-9a-fA-F]{6})")]
    private static partial Regex VariableDeclaration();
}
