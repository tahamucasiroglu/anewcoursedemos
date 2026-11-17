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
/// Yeni sürüm ve yeniden üret, Web tarafı (spec 0020 AC-17): Üretilen Videolar'da Yeni sürüm alanı (etiketli kontroller), süren sürüm kutusu,
/// Yeniden üret, onay pencereleri (alertdialog, aria-modal); Konseptler'de Yeniden üret paneli ve onayı; Ajax uçları giriş ve CSRF ister, istek
/// Api'ye (bağlam belirteci dahil) iletilir, Api'nin hata mesajı taşınır.
/// </summary>
public sealed class VideoVersionPageTests
{
    /// <summary>AC-17: kontrollerin erişilebilir adları, durum bölgeleri, onay pencereleri ve resx metinleri.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProducedPage_VersionControls()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/uretilenler", ct);

        page.GetElementById("produced-regenerate")!.TextContent.ShouldBe("Yeniden üret");
        page.GetElementById("version-heading")!.TextContent.ShouldBe("Yeni sürüm");
        foreach (var (id, label) in new[] { ("version-engine", "Video motoru"), ("version-image", "Görsel modeli"), ("version-audio", "Ses motoru") })
        {
            page.QuerySelector($"label[for='{id}']")!.TextContent.ShouldBe(label);
        }

        page.GetElementById("version-subtitles")!.ParentElement!.TextContent.Trim().ShouldBe("Altyazı");
        page.GetElementById("version-progress")!.ParentElement!.TextContent.Trim().ShouldBe("İlerleme çubuğu");
        page.GetElementById("version-preview")!.GetAttribute("role").ShouldBe("status");
        page.GetElementById("version-start")!.GetAttribute("type").ShouldBe("button");
        page.GetElementById("produced-active-retry")!.TextContent.ShouldBe("Tekrar dene");
        page.GetElementById("produced-active-remove")!.TextContent.ShouldBe("Sürümü kaldır");
        foreach (var dialog in new[] { "version-confirm-dialog", "version-remove-dialog" })
        {
            var box = page.GetElementById(dialog)!.QuerySelector("[role='alertdialog']")!;
            box.GetAttribute("aria-modal").ShouldBe("true");
            box.QuerySelector("[data-confirm-cancel]").ShouldNotBeNull();
            box.QuerySelector("[data-confirm-ok]").ShouldNotBeNull();
        }

        page.GetElementById("version-confirm-warning")!.TextContent.ShouldContain("Tahmini tutar gösterilmez");
        var root = page.GetElementById("produced")!;
        root.GetAttribute("data-text-delete-blocked").ShouldBe("Önce üretimi durdurun.");
        root.GetAttribute("data-text-version-busy").ShouldBe("Bu video için zaten üretimde bir sürüm var.");
        root.GetAttribute("data-text-version-offline").ShouldBe("Online model çağrısı yapılmayacak.");
        root.GetAttribute("data-text-status-waiting_for_agent").ShouldBe("Ajan bekleniyor");

        var concepts = await WebFactory.GetDocumentAsync(client, "/", ct); // Konseptler ana sayfadır.
        var regen = concepts.GetElementById("regen-panel")!;
        regen.HasAttribute("hidden").ShouldBeTrue();
        regen.GetAttribute("data-text-unchanged").ShouldBe("Konsept değişmedi.");
        concepts.GetElementById("regen-force")!.ParentElement!.TextContent.Trim().ShouldBe("Yine de baştan üret");
        concepts.GetElementById("regen-confirm-dialog")!.QuerySelector("[role='alertdialog']")!.GetAttribute("aria-modal").ShouldBe("true");
    }

    /// <summary>AC-15/AC-17: uçlar giriş ister, POST CSRF ister; istek ve belirteç Api'ye iletilir; Api'nin 409/404 cevabı taşınır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VersionEndpoints_RequireAuthAndCsrf_Forward()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var video = Guid.CreateVersion7();
        var version = Guid.CreateVersion7();
        using var ok = JsonDocument.Parse("""{"stages":["voice","merge"],"online":false,"unchanged":false}""");
        factory.Api.SendVideoVersionQueryAsync(video, Arg.Any<VideoVersionQuery>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(ok.RootElement.Clone()));
        factory.Api.SendProductionAsync(ProductionCommand.StartVersion, video, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(new Error("production.version_active", "Bu video için zaten üretimde bir sürüm var.", ErrorType.Conflict)));
        factory.Api.SendProductionAsync(ProductionCommand.RemoveFailedVersion, video, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Failure(new Error("production.video_not_found", "Video bulunamadı.", ErrorType.NotFound)));
        factory.Api.SendConceptEditInContextAsync(video, ConceptEditCommand.SaveScenes, Arg.Any<JsonElement?>(), "belirtec", Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(ok.RootElement.Clone()));

        using (var anonymous = SignedInClient.CreateAnonymous(factory))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/uretilenler/video/{video}/yeni-surum");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            (await anonymous.SendAsync(request, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsJsonAsync($"/uretilenler/video/{video}/yeni-surum", new { baseVersionId = version }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync($"/uretilenler/video/{video}/yeniden-uret/ac", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().SendProductionAsync(ProductionCommand.StartVersion, Arg.Any<Guid?>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().SendVideoVersionQueryAsync(
            Arg.Any<Guid>(), VideoVersionQuery.OpenRegeneration, Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());

        (await client.GetAsync(new Uri($"/uretilenler/video/{video}/yeni-surum?taban={version}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).SendVideoVersionQueryAsync(video, VideoVersionQuery.Options, version, null, Arg.Any<CancellationToken>());
        (await client.GetAsync(new Uri($"/uretilenler/video/{video}/yeniden-uret?belirtec=abc&taban={version}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).SendVideoVersionQueryAsync(video, VideoVersionQuery.RegenerationPreview, version, "abc", Arg.Any<CancellationToken>());

        var busy = await PostAjaxAsync(client, $"/uretilenler/video/{video}/yeni-surum", new { baseVersionId = version, audioModel = "local/chatterbox-multilingual-v3" }, ct);
        busy.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await busy.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("message").GetString().ShouldBe("Bu video için zaten üretimde bir sürüm var.");
        await factory.Api.Received(1).SendProductionAsync(
            ProductionCommand.StartVersion, video, Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("audioModel").GetString() == "local/chatterbox-multilingual-v3"),
            Arg.Any<CancellationToken>());
        (await PostAjaxAsync(client, $"/uretilenler/video/{video}/surumu-kaldir", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAjaxAsync(client, $"/uretilenler/video/{video}/yeniden-uret/ac?taban={version}", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).SendVideoVersionQueryAsync(video, VideoVersionQuery.OpenRegeneration, version, null, Arg.Any<CancellationToken>());
        (await PostAjaxAsync(client, $"/konseptler/{video}/baglam/sahneler?regen=belirtec", new { scenes = Array.Empty<object>() }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).SendConceptEditInContextAsync(video, ConceptEditCommand.SaveScenes, Arg.Any<JsonElement?>(), "belirtec", Arg.Any<CancellationToken>());
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
