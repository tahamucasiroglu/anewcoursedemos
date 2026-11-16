using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Üretilen Videolar, Web tarafı (spec 0007 AC-3, AC-11, AC-12, AC-13): üç sütunlu sayfa, yer tutucular, erişilebilir düğmeler ve resx metinleri;
/// Ajax uçları giriş ister, POST CSRF korumalıdır, istek Api'ye iletilir ve Api'nin "bulunamadı"/"zaman aşımı" cevabı taşınır.
/// </summary>
public sealed class ProducedPageTests
{
    /// <summary>AC-3/AC-13: liste, izleme yeri, sağ panel; yer tutucular; iki dosya eylemi düğme; oynatıcı yerel kontrollü; resx metinleri.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProducedPage_Layout_States()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/uretilenler", ct);

        page.GetElementById("profile-required")!.TextContent.ShouldBe("Önce bir profil oluşturun");
        var root = page.GetElementById("produced")!;
        root.HasAttribute("data-requires-profile").ShouldBeTrue();
        root.Children.Select(c => c.ClassName).ShouldBe(["produced__list", "produced__viewer", "produced__panel"]);
        page.GetElementById("produced-empty")!.TextContent.ShouldBe("Henüz üretilmiş video yok.");
        page.GetElementById("produced-viewer-status")!.TextContent.ShouldBe("İzlemek için soldan bir video seçin");
        page.GetElementById("produced-viewer-status")!.GetAttribute("role").ShouldBe("status");
        page.GetElementById("produced-panel-hint")!.TextContent.ShouldBe("Ayrıntılar için bir video seçin");
        page.GetElementById("produced-more")!.TextContent.ShouldBe("Daha fazla göster");
        page.GetElementById("produced-retry")!.TextContent.ShouldBe("Yeniden dene");
        page.GetElementById("produced-player")!.HasAttribute("controls").ShouldBeTrue();
        page.GetElementById("produced-open-folder")!.TextContent.ShouldBe("Klasörde aç");
        page.GetElementById("produced-open-player")!.TextContent.ShouldBe("Sistem oynatıcısında aç");
        page.QuerySelectorAll(".produced__actions button").ShouldAllBe(b => b.GetAttribute("type") == "button");
        page.QuerySelectorAll(".produced__subheading").Select(h => h.TextContent)
            .ShouldBe(["Seslendirme ve sahneler", "Konsept metni", "Ayrıntılar ve ücret", "Video sürümleri"]);
        root.GetAttribute("data-text-agent-offline").ShouldBe("Videoyu izlemek için ajan bağlı olmalı");
        root.GetAttribute("data-text-other-device").ShouldBe("Bu video yalnızca ajanın kurulu olduğu bilgisayarda ({0}) izlenebilir");
        root.GetAttribute("data-text-file-missing").ShouldBe("Video dosyası bulunamadı");
        root.GetAttribute("data-text-free").ShouldBe("Yerel — ücretsiz");
        root.GetAttribute("data-text-ai-voice").ShouldBe("Yapay zekâ ile üretilmiş ses");
        root.GetAttribute("data-text-timeout").ShouldBe("Zaman aşımı");
        root.GetAttribute("data-text-status-completed").ShouldBe("Tamamlandı");
    }

    /// <summary>AC-11/AC-12: uçlar giriş ister, POST CSRF ister; istek Api'ye iletilir; Api'nin 404 ve 504 cevabı Türkçe mesajla taşınır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProducedEndpoints_RequireAuthAndCsrf()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var profile = Guid.CreateVersion7();
        var video = Guid.CreateVersion7();
        var version = Guid.CreateVersion7();
        using var page = JsonDocument.Parse("""{"items":[],"hasMore":false}""");
        factory.Api.ListProducedAsync(profile, 50, Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(page.RootElement.Clone()));
        factory.Api.SendProductionAsync(ProductionCommand.ProducedVideo, video, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(new Error("production.video_not_found", "Video bulunamadı.", ErrorType.NotFound)));
        factory.Api.SendProductionAsync(ProductionCommand.OpenFile, version, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(new Error("production.file_action_timeout", "Zaman aşımı", ErrorType.Timeout)));

        using (var anonymous = SignedInClient.CreateAnonymous(factory))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/uretilenler/{profile}/liste");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            (await anonymous.SendAsync(request, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsJsonAsync($"/uretilenler/surum/{version}/ac", new { action = "folder" }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().SendProductionAsync(ProductionCommand.OpenFile, Arg.Any<Guid?>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());

        (await client.GetAsync(new Uri($"/uretilenler/{profile}/liste?atla=50", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).ListProducedAsync(profile, 50, Arg.Any<CancellationToken>());

        var missing = await client.GetAsync(new Uri($"/uretilenler/video/{video}", UriKind.Relative), ct);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await missing.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("message").GetString().ShouldBe("Video bulunamadı.");

        var timeout = await PostAjaxAsync(client, $"/uretilenler/surum/{version}/ac", new { action = "player" }, ct);
        timeout.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        (await timeout.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("message").GetString().ShouldBe("Zaman aşımı");
        await factory.Api.Received(1).SendProductionAsync(
            ProductionCommand.OpenFile, version, Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("action").GetString() == "player"), Arg.Any<CancellationToken>());
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
}
