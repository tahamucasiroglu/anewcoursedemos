using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Günlük şeridi ve geçmiş modalı Web tarafı (spec 0021 AC-5, AC-8): işaretleme, giriş sayfasında şerit yok, Ajax ucu.
/// </summary>
public sealed class JournalPageTests : IClassFixture<WebFactory>
{
    private readonly WebFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="JournalPageTests"/> class.</summary>
    /// <param name="factory">Web fabrikası.</param>
    public JournalPageTests(WebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Giriş yapmışken şerit klavyeyle erişilebilir bir düğmedir (yükleniyor durumu, ayrı polite duyuru alanı) ve geçmiş modalı
    /// etiketli bir diyalogdur; giriş sayfasında Günlük yoktur.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Layout_SignedIn_JournalStripAndModal_LoginHasNone()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateSignedInClientAsync(ct);
        using var anonymous = SignedInClient.CreateAnonymous(_factory);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var login = await WebFactory.GetDocumentAsync(anonymous, "/giris", ct);

        var strip = page.QuerySelector("button#journal")!;
        strip.GetAttribute("type").ShouldBe("button");
        strip.GetAttribute("data-state").ShouldBe("loading");
        strip.GetAttribute("aria-controls").ShouldBe("journal-modal");
        page.QuerySelector("#journal-text")!.TextContent.ShouldBe("Yükleniyor…");
        strip.GetAttribute("data-text-empty").ShouldBe("Henüz bir işlem yok");
        strip.GetAttribute("data-text-warning").ShouldBe("Uyarı");
        page.QuerySelector("#journal-stale")!.TextContent.ShouldBe("Günlük güncellenemiyor");
        page.QuerySelector("#journal-announce")!.GetAttribute("aria-live").ShouldBe("polite");
        strip.GetAttribute("aria-live").ShouldBeNull();
        var dialog = page.QuerySelector("#journal-modal [role='dialog']")!;
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        page.QuerySelector("#journal-modal-title")!.TextContent.ShouldBe("Günlük geçmişi");
        page.QuerySelectorAll("#journal-filter option").Select(o => o.TextContent).ShouldBe(["Tümü", "Uyarı", "Hata"]);
        page.QuerySelector("#journal-prev")!.TextContent.ShouldBe("Önceki gün");
        page.QuerySelector("#journal-next")!.TextContent.ShouldBe("Sonraki gün");
        page.QuerySelector("#journal-more")!.TextContent.ShouldBe("Daha fazla");
        login.QuerySelector("#journal").ShouldBeNull();
        login.QuerySelector("#journal-modal").ShouldBeNull();
    }

    /// <summary>
    /// <c>/gunluk</c> tarih, filtre ve atlanacak kaydı Api'ye iletir, gün sayfasını döndürür; giriş yapılmamış istek girişe gider.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task JournalAjax_ForwardsQuery()
    {
        var ct = TestContext.Current.CancellationToken;
        _factory.Api.GetJournalDayAsync("2026-11-15", "error", 50, Arg.Any<CancellationToken>()).Returns(Result<JournalDayResponse>.Success(
            new JournalDayResponse("2026-11-15", false, true, [new SystemEventResponse("t", 2, "<b>hata</b>", DateTimeOffset.UnixEpoch, Guid.NewGuid())], false)));
        _factory.Api.GetJournalDayAsync(null, null, 0, Arg.Any<CancellationToken>())
            .Returns(Result<JournalDayResponse>.Failure(ApiClientErrors.Unavailable));
        using var client = await _factory.CreateSignedInClientAsync(ct);
        using var anonymous = SignedInClient.CreateAnonymous(_factory);

        var body = await client.GetFromJsonAsync<JsonElement>("/gunluk?tarih=2026-11-15&filtre=error&atla=50", ct);
        using var down = await client.GetAsync(new Uri("/gunluk", UriKind.Relative), ct);
        using var redirect = await anonymous.GetAsync(new Uri("/gunluk", UriKind.Relative), ct);

        body.GetProperty("date").GetString().ShouldBe("2026-11-15");
        body.GetProperty("isFirstDay").GetBoolean().ShouldBeTrue();
        body.GetProperty("items")[0].GetProperty("message").GetString().ShouldBe("<b>hata</b>");
        down.IsSuccessStatusCode.ShouldBeFalse();
        redirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        redirect.Headers.Location!.AbsolutePath.ShouldBe("/giris");
    }
}
