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
/// Video silme ve Silinenler — Videolar, Web tarafı (spec 0019 AC-1, AC-3, AC-5, AC-9, AC-10): Üretilen Videolar'da Sil düğmesi ve onay
/// diyaloğu, Silinenler'in Videolar sekmesi (boş durum, durum alanı, Yeniden dene, kalıcı silme onayı, resx metinleri); Ajax uçları giriş
/// ister, POST'lar CSRF korumalıdır ve istek Api'ye iletilir (Api'nin 404/409'u taşınır).
/// </summary>
public sealed class DeletedVideosPageTests
{
    /// <summary>AC-1/AC-3/AC-5/AC-10: düğmeler, diyaloglar, boş durum, durum alanı ve metinler; geri yükleme kontrolü yok.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task DeletedVideosPage_TabAndStates()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/silinenler", ct);
        var panel = page.GetElementById("deleted-panel-videos")!;
        panel.GetAttribute("role").ShouldBe("tabpanel");
        page.GetElementById("deleted-videos-empty")!.TextContent.ShouldBe("Henüz silinmiş video yok");
        page.GetElementById("deleted-videos-status")!.GetAttribute("role").ShouldBe("status");
        page.GetElementById("deleted-videos-retry")!.TextContent.ShouldBe("Yeniden dene");
        panel.GetAttribute("data-text-agent-offline").ShouldBe("Ajan bağlı değil");
        panel.GetAttribute("data-text-partial").ShouldBe("Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin");
        panel.GetAttribute("data-text-purge").ShouldBe("Videoyu kalıcı sil: {0}");
        var dialog = page.QuerySelector("#deleted-video-purge-dialog [role='alertdialog']")!;
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        page.GetElementById("deleted-video-purge-text")!.TextContent
            .ShouldBe("Video kaydı ve tüm sürüm dosyaları bilgisayarınızdan kalıcı olarak silinecek, geri alınamaz.");
        dialog.QuerySelector("[data-confirm-cancel]")!.TagName.ShouldBe("BUTTON");
        page.Body!.TextContent.ShouldNotContain("Geri yükle");

        var produced = await WebFactory.GetDocumentAsync(client, "/uretilenler", ct);
        var delete = produced.GetElementById("produced-delete")!;
        delete.TagName.ShouldBe("BUTTON");
        delete.TextContent.ShouldBe("Sil");
        delete.ParentElement!.Id.ShouldBe("produced-actions");
        produced.GetElementById("produced")!.GetAttribute("data-text-delete-label").ShouldBe("Videoyu sil: {0}");
        produced.QuerySelector("#produced-delete-dialog [role='alertdialog']")!.GetAttribute("aria-modal").ShouldBe("true");
        produced.GetElementById("produced-delete-text")!.TextContent.ShouldBe("Video tüm sürümleriyle Silinenler'e taşınacak; dosyalar bilgisayarınızda kalır.");
    }

    /// <summary>AC-9/AC-10: uçlar giriş ister, POST CSRF ister; istek Api'ye iletilir ve Api'nin 404'ü ile 409 mesajı taşınır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task VideoTrash_AjaxEndpoints_RequireAuthAndCsrf()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var video = Guid.CreateVersion7();
        var profile = Guid.CreateVersion7();
        using var list = JsonDocument.Parse("""{"items":[],"page":1,"pageSize":50,"total":0}""");
        var notFound = Result<JsonElement>.Failure(new Error("production.video_not_found", "Video bulunamadı.", ErrorType.NotFound));
        var partial = Result<JsonElement>.Failure(new Error("production.purge_partial", "Bazı dosyalar silinemedi, dosyayı kapatıp tekrar deneyin", ErrorType.Conflict));
        factory.Api.ListDeletedVideosAsync(profile, 2, Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(list.RootElement.Clone()));
        factory.Api.SendProductionAsync(ProductionCommand.DeleteVideo, video, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(notFound);
        factory.Api.SendProductionAsync(ProductionCommand.PurgeVideo, video, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(partial);

        using (var anonymous = SignedInClient.CreateAnonymous(factory))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/silinenler/videolar/{profile}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            (await anonymous.SendAsync(request, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await factory.Api.DidNotReceive().ListDeletedVideosAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        using var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsJsonAsync($"/uretilenler/video/{video}/sil", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync($"/silinenler/videolar/{video}/kalici-sil", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().SendProductionAsync(Arg.Any<ProductionCommand>(), Arg.Any<Guid?>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());

        (await client.GetAsync(new Uri($"/silinenler/videolar/{profile}?sayfa=2", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, $"/uretilenler/video/{video}/sil", ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var purged = await PostAjaxAsync(client, $"/silinenler/videolar/{video}/kalici-sil", ct);
        purged.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await purged.Content.ReadAsStringAsync(ct)).ShouldContain("production.purge_partial");
        await factory.Api.Received(1).SendProductionAsync(ProductionCommand.DeleteVideo, video, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendProductionAsync(ProductionCommand.PurgeVideo, video, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
    }

    private static async Task<HttpResponseMessage> PostAjaxAsync(HttpClient client, string path, CancellationToken ct)
    {
        var page = await WebFactory.GetDocumentAsync(client, "/yayinlama", ct);
        var token = page.QuerySelector("meta[name='csrf-token']")!.GetAttribute("content")!;
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(new { }) };
        request.Headers.Add("RequestVerificationToken", token);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        return await client.SendAsync(request, ct);
    }
}
