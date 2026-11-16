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
/// Konseptten Video, Web tarafı (spec 0006 AC-15, AC-20, AC-21, AC-22): sayfa yerleşimi, boş durumlar, modal ve erişilebilir adlar, resx
/// metinleri; Ajax uçları giriş ister, POST'lar CSRF korumalıdır ve istek Api'ye iletilir (Api'nin 404'ü taşınır).
/// </summary>
public sealed class ProductionPageTests
{
    /// <summary>AC-15/AC-21/AC-22: şerit, listeler, boş durumlar, modal rolleri ve etiketleri; "Önce bir profil oluşturun".</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ProductionPage_States()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/konseptten-video", ct);

        page.GetElementById("profile-required")!.TextContent.ShouldBe("Önce bir profil oluşturun");
        page.GetElementById("production")!.HasAttribute("data-requires-profile").ShouldBeTrue();
        page.GetElementById("production-approved-empty")!.TextContent.ShouldBe("Onaylanmış konsept yok — Konseptler sayfasından video için konsept seçin");
        page.GetElementById("production-queue-empty")!.TextContent.ShouldBe("Sıra boş");
        page.GetElementById("production-detail-hint")!.TextContent.ShouldBe("Ayrıntı için bir konsepte tıklayın");
        page.GetElementById("production-start")!.TextContent.ShouldBe("Sırayla üret");
        page.GetElementById("production-start")!.HasAttribute("disabled").ShouldBeTrue();
        page.GetElementById("production-enqueue")!.HasAttribute("disabled").ShouldBeTrue();
        page.GetElementById("production-stop")!.TextContent.ShouldBe("Durdur");
        page.GetElementById("production-subtitles-note")!.TextContent.ShouldBe("Sessiz videoda altyazı yok");
        page.GetElementById("production-status")!.GetAttribute("role").ShouldBe("status");
        page.QuerySelector("label[for='production-model-code']")!.TextContent.ShouldBe("Video kodu modeli");
        page.QuerySelector("label[for='production-model-engine']")!.TextContent.ShouldBe("Video motoru");
        page.QuerySelector("label[for='production-model-image']")!.TextContent.ShouldBe("Görsel modeli");
        page.QuerySelector("label[for='production-model-audio']")!.TextContent.ShouldBe("Ses modeli");
        page.QuerySelector("#production-models-modal [role='dialog']")!.GetAttribute("aria-modal").ShouldBe("true");
        page.GetElementById("production-models-open")!.GetAttribute("aria-haspopup").ShouldBe("dialog");
        page.GetElementById("production-models-close")!.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        page.GetElementById("production-models-error")!.TextContent.ShouldBe("Model listesi yüklenemedi. Biraz sonra tekrar deneyin.");
        var root = page.GetElementById("production")!;
        root.GetAttribute("data-text-image-off").ShouldBe("Kapalı — koddan çiz");
        root.GetAttribute("data-text-audio-off").ShouldBe("Kapalı — sessiz video");
        root.GetAttribute("data-text-only-turkish").ShouldBe("Yalnız Türkçe");
        root.GetAttribute("data-text-license").ShouldBe("Lisans uyarısı");
        root.GetAttribute("data-text-move-up").ShouldBe("{0}'ı yukarı taşı");
        root.GetAttribute("data-text-resume").ShouldBe("Kaldığı yerden devam et");
        root.GetAttribute("data-text-edit-in-concepts").ShouldBe("Konseptler'de düzenle");
        root.GetAttribute("data-text-failed").ShouldBe("Sunucuya ulaşılamadı. Biraz sonra tekrar deneyin.");
        new[] { "queued", "producing", "stopped", "waiting-for-agent", "failed", "completed" }.Select(s => root.GetAttribute($"data-text-status-{s}"))
            .ShouldBe(["Sırada", "Üretiliyor", "Durduruldu", "Ajan bekleniyor", "Hata", "Tamamlandı"]);
    }

    /// <summary>AC-20/AC-22: uçlar giriş ister, POST CSRF ister; istek Api'ye iletilir ve Api'nin 404'ü taşınır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Production_AjaxEndpoints_RequireAuthAndCsrf()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var profile = Guid.CreateVersion7();
        var job = Guid.CreateVersion7();
        using var state = JsonDocument.Parse("""{"profileId":"00000000-0000-0000-0000-000000000000","jobs":[]}""");
        var ok = Result<JsonElement>.Success(state.RootElement.Clone());
        var notFound = Result<JsonElement>.Failure(new Error("production.job_not_found", "İş bulunamadı.", ErrorType.NotFound));
        factory.Api.GetProductionStateAsync(profile, Arg.Any<CancellationToken>()).Returns(ok);
        factory.Api.SendProductionAsync(Arg.Any<ProductionCommand>(), Arg.Any<Guid?>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(ok);
        factory.Api.SendProductionAsync(ProductionCommand.Remove, job, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(notFound);

        using (var anonymous = SignedInClient.CreateAnonymous(factory))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/uretim/{profile}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            (await anonymous.SendAsync(request, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await factory.Api.DidNotReceive().GetProductionStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        using var client = await factory.CreateSignedInClientAsync(ct);
        foreach (var path in new[] { $"/uretim/{profile}/ayarlar", "/uretim/sira", "/uretim/baslat", "/uretim/durdur", $"/uretim/is/{job}/tasi", $"/uretim/is/{job}/cikar", $"/uretim/is/{job}/devam" })
        {
            (await client.PostAsJsonAsync(path, new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest, path);
        }

        await factory.Api.DidNotReceive().SendProductionAsync(Arg.Any<ProductionCommand>(), Arg.Any<Guid?>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());

        (await client.GetAsync(new Uri($"/uretim/{profile}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, $"/uretim/{profile}/ayarlar", new { subtitles = true }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, "/uretim/sira", new { profileId = profile, conceptIds = new[] { job } }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, "/uretim/baslat", new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, "/uretim/durdur", new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, $"/uretim/is/{job}/tasi", new { direction = "up" }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, $"/uretim/is/{job}/devam", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, $"/uretim/is/{job}/cikar", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await factory.Api.Received(1).SendProductionAsync(ProductionCommand.SaveSettings, profile, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendProductionAsync(
            ProductionCommand.Enqueue, null, Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("profileId").GetGuid() == profile), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendProductionAsync(ProductionCommand.Start, null, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendProductionAsync(ProductionCommand.Stop, null, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendProductionAsync(
            ProductionCommand.Move, job, Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("direction").GetString() == "up"), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendProductionAsync(ProductionCommand.Resume, job, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendProductionAsync(ProductionCommand.Remove, job, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
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
