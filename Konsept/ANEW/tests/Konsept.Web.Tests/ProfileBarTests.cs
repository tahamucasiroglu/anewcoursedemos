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
/// Profiller, Web tarafı (spec 0003 AC-2, AC-12, AC-13, AC-15, AC-18): üst bardaki profil çubuğu sunucuda yükleniyor durumuyla
/// gelir, kalem pasif + nedenli, artı etkin; profil modalının alanları etiketli ve hata metinlerine bağlıdır; profil gerektiren
/// sayfalarda "Önce bir profil oluşturun" boş durumu vardır; Ajax uçları giriş ister, CSRF korumalıdır ve Api'ye iletir.
/// </summary>
public sealed partial class ProfileBarTests
{
    /// <summary>AC-13/AC-18: profil çubuğu yükleniyor durumunda gelir; kalem pasif ve nedenli; artı etkin; erişilebilir adlar.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProfileBar_ServerRendersLoadingState()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var home = await WebFactory.GetDocumentAsync(client, "/", ct);
        var actions = home.QuerySelector(".topbar__actions")!;

        actions.FirstElementChild!.Id.ShouldBe("profile-bar");
        var bar = home.GetElementById("profile-bar")!;
        bar.GetAttribute("data-state").ShouldBe("loading");
        home.GetElementById("profile-status")!.TextContent.ShouldBe("Profiller yükleniyor…");
        var select = home.GetElementById("profile-select")!;
        select.LocalName.ShouldBe("select");
        select.GetAttribute("aria-label").ShouldBe("Profil seç");
        var edit = home.GetElementById("profile-edit")!;
        edit.GetAttribute("aria-disabled").ShouldBe("true");
        edit.GetAttribute("aria-label").ShouldBe("Seçili profili düzenle");
        edit.GetAttribute("title").ShouldBe("Profiller yüklenmeden düzenlenemez");
        var add = home.GetElementById("profile-new")!;
        add.GetAttribute("aria-label").ShouldBe("Yeni profil");
        add.HasAttribute("aria-disabled").ShouldBeFalse();
        home.GetElementById("profile-create")!.TextContent.ShouldBe("Profil oluşturun");

        foreach (var path in new[] { "/", "/konseptten-video", "/uretilenler", "/silinenler" })
        {
            var page = await WebFactory.GetDocumentAsync(client, path, ct);
            page.GetElementById("profile-required")!.TextContent.ShouldBe("Önce bir profil oluşturun", path);
            page.QuerySelector(".empty-state")!.HasAttribute("data-requires-profile").ShouldBeTrue(path);
        }

        var publishing = await WebFactory.GetDocumentAsync(client, "/yayinlama", ct);
        publishing.GetElementById("profile-required").ShouldBeNull();
        publishing.QuerySelector("[data-requires-profile]").ShouldBeNull();
    }

    /// <summary>
    /// AC-2/AC-18: modalda kanal adı, video dili (Türkçe, İngilizce), biçim (yatay/dikey), hedef süre (dakika + saniye) ve kanal
    /// içeriği vardır; örnek görsel alanı yoktur; her alanın görünür etiketi ve bağlı hata metni vardır; modal ve onay penceresi
    /// erişilebilir rol ve adlıdır; onay metinleri resx'tedir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProfileModal_FieldsAndLabels()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var modal = page.GetElementById("profile-modal")!;

        modal.HasAttribute("hidden").ShouldBeTrue();
        var dialog = modal.QuerySelector("[role='dialog']")!;
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        page.GetElementById(dialog.GetAttribute("aria-labelledby")!).ShouldNotBeNull();
        foreach (var (id, label) in new[]
        {
            ("profile-template", "Hazır şablon"), ("profile-name", "Kanal adı"), ("profile-language", "Video dili"),
            ("profile-minutes", "Dakika"), ("profile-seconds", "Saniye"), ("profile-content", "Kanal içeriği"),
        })
        {
            page.QuerySelector($"label[for='{id}']")!.TextContent.Trim().ShouldBe(label);
        }

        page.GetElementById("profile-language")!.QuerySelectorAll("option").Select(o => (o.GetAttribute("value"), o.TextContent))
            .ShouldBe([("tr", "Türkçe"), ("en", "İngilizce")]);
        modal.QuerySelectorAll("input[name='format']").Select(i => i.GetAttribute("value")).ShouldBe(["landscape", "portrait"]);
        modal.QuerySelectorAll("legend").Select(l => l.TextContent.Trim()).ShouldBe(["Biçim", "Hedef süre"]);
        modal.QuerySelectorAll("input[type='file'], img, [name*='image' i]").Length.ShouldBe(0);
        foreach (var id in new[] { "profile-name", "profile-language", "profile-minutes", "profile-seconds", "profile-content" })
        {
            var describedBy = page.GetElementById(id)!.GetAttribute("aria-describedby")!.Split(' ');
            describedBy.ShouldContain(d => d.EndsWith("-error", StringComparison.Ordinal), id);
            describedBy.ShouldAllBe(d => page.GetElementById(d) != null);
        }

        modal.GetAttribute("data-text-confirm-template").ShouldBe("Doldurduğunuz alanlar şablonla değiştirilecek");
        modal.GetAttribute("data-text-confirm-discard").ShouldBe("Değişiklikler kaydedilmeyecek");
        modal.GetAttribute("data-text-confirm-delete").ShouldBe("Bu profil kalıcı olarak silinecek");
        page.GetElementById("profile-bar")!.GetAttribute("data-text-limit")!.Replace("{0}", "20", StringComparison.Ordinal)
            .ShouldBe("En fazla 20 profil oluşturabilirsiniz");
        var confirm = page.QuerySelector("#confirm-dialog [role='alertdialog']")!;
        confirm.GetAttribute("aria-modal").ShouldBe("true");
        confirm.QuerySelectorAll("button").Select(b => b.TextContent).ShouldBe(["Vazgeç", "Onayla"]);
        page.GetElementById("profile-modal-close")!.GetAttribute("aria-label").ShouldBe("Kapat");
    }

    /// <summary>AC-18: modal, üst bar ve onay penceresinin renk çiftleri iki temada da WCAG AA (≥ 4.5).</summary>
    [Fact]
    public void ProfileBar_ContrastAA()
    {
        var css = File.ReadAllText(Path.Combine(WebPaths.WebProject, "wwwroot", "css", "site.css"));
        var themes = VariableBlock().Matches(css).Select(m => Variables(m.Groups["body"].Value)).ToList();

        themes.Count.ShouldBe(2);
        foreach (var (fore, back) in new[]
        {
            ("--color-text", "--color-surface"), ("--color-text-muted", "--color-surface"), ("--color-error", "--color-surface"),
            ("--color-on-primary", "--color-primary"), ("--color-text", "--color-surface-alt"), ("--color-text-muted", "--color-surface-alt"),
        })
        {
            foreach (var theme in themes)
            {
                Contrast(theme[fore], theme[back]).ShouldBeGreaterThanOrEqualTo(4.5, $"{fore} / {back}");
            }
        }
    }

    /// <summary>AC-15: girişsiz kullanıcı profil uçlarını kullanamaz (401); Api çağrılmaz.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Profiles_Anonymous_Unauthorized()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = SignedInClient.CreateAnonymous(factory);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/profiller");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        using var response = await client.SendAsync(request, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await factory.Api.DidNotReceive().GetProfilesAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// AC-1/AC-7/AC-11: uçlar CSRF ister; oluşturma, seçim, düzenleme ve silme Api'ye aynen iletilir; Api'nin alan hatası alan adıyla,
    /// sınır hatası mesajıyla döner.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Profiles_ForwardToApi_WithCsrf()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        var id = Guid.CreateVersion7();
        var created = new ProfileResponse(id, "Kanal", "tr", "portrait", 90, "İçerik", DateTimeOffset.UtcNow, true);
        factory.Api.CreateProfileAsync(Arg.Any<SaveProfileApiRequest?>(), Arg.Any<CancellationToken>()).Returns(Result<ProfileResponse>.Success(created));
        factory.Api.SelectProfileAsync(id, Arg.Any<CancellationToken>()).Returns(Result<ProfileResponse>.Success(created));
        factory.Api.UpdateProfileAsync(id, Arg.Any<SaveProfileApiRequest?>(), Arg.Any<CancellationToken>()).Returns(Result<ProfileResponse>.Failure(
            new Error("profiles.invalid", "Girilen bilgiler geçersiz.", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]> { ["ChannelName"] = ["Bu kanal adını başka bir profilinizde kullanıyorsunuz."] },
            }));
        factory.Api.DeleteProfileAsync(id, Arg.Any<CancellationToken>()).Returns(Result<ProfileListResponse>.Failure(
            new Error("profiles.not_found", "Profil bulunamadı.", ErrorType.NotFound)));
        var form = new { channelName = "Kanal", language = "tr", format = "portrait", durationSeconds = 90, content = "İçerik" };

        using var withoutToken = await client.PostAsJsonAsync("/profiller", form, ct);
        withoutToken.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().CreateProfileAsync(Arg.Any<SaveProfileApiRequest?>(), Arg.Any<CancellationToken>());

        using var create = await PostAjaxAsync(client, "/profiller", form, ct);
        using var select = await PostAjaxAsync(client, "/profiller/secili", new { profileId = id }, ct);
        using var update = await PostAjaxAsync(client, $"/profiller/{id}", form, ct);
        using var delete = await PostAjaxAsync(client, $"/profiller/{id}/sil", new { }, ct);

        create.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonDocument.Parse(await create.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("id").GetGuid().ShouldBe(id);
        await factory.Api.Received(1).CreateProfileAsync(new SaveProfileApiRequest("Kanal", "tr", "portrait", 90, "İçerik"), Arg.Any<CancellationToken>());
        select.StatusCode.ShouldBe(HttpStatusCode.OK);
        update.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        JsonDocument.Parse(await update.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("errors").GetProperty("ChannelName")[0].GetString()
            .ShouldBe("Bu kanal adını başka bir profilinizde kullanıyorsunuz.");
        delete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        JsonDocument.Parse(await delete.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("message").GetString().ShouldBe("Profil bulunamadı.");
    }

    private static async Task<HttpResponseMessage> PostAjaxAsync(HttpClient client, string path, object body, CancellationToken ct)
    {
        var page = await WebFactory.GetDocumentAsync(client, "/yayinlama", ct);
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
