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
/// Silinenler ve önerileri sıfırla, Web tarafı (spec 0016 AC-3, AC-6, AC-9, AC-11, AC-12): sekmeler ve boş durumlar, onay diyalogları ve
/// resx metinleri; Ajax uçları giriş ister, POST'lar CSRF korumalıdır ve istek Api'ye iletilir (404 taşınır).
/// </summary>
public sealed class DeletedPageTests
{
    /// <summary>AC-3/AC-6/AC-12: sekme rolleri, boş durumlar, kalıcı silme onayı; geri yükleme kontrolü yok; çöp ve sıfırlama onayları.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task DeletedPage_TabsAndStates()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/silinenler", ct);
        page.QuerySelector("[role='tablist']").ShouldNotBeNull();
        var tabs = page.QuerySelectorAll("[role='tab']");
        tabs.Select(t => t.TextContent).ShouldBe(["Konseptler", "Videolar"]);
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        tabs[1].GetAttribute("tabindex").ShouldBe("-1");
        foreach (var tab in tabs)
        {
            var panel = page.GetElementById(tab.GetAttribute("aria-controls")!)!;
            panel.GetAttribute("role").ShouldBe("tabpanel");
            panel.GetAttribute("aria-labelledby").ShouldBe(tab.Id);
        }

        page.GetElementById("deleted-empty")!.TextContent.ShouldBe("Henüz silinmiş konsept yok");
        page.QuerySelector("#deleted-panel-videos .empty-state")!.TextContent.ShouldBe("Henüz silinmiş video yok");
        page.GetElementById("deleted-panel-videos")!.HasAttribute("hidden").ShouldBeTrue();
        page.GetElementById("deleted-status")!.GetAttribute("role").ShouldBe("status");
        var root = page.GetElementById("deleted")!;
        root.GetAttribute("data-text-load-failed").ShouldBe("Silinenler alınamadı. Biraz sonra tekrar deneyin.");
        root.GetAttribute("data-text-purge").ShouldBe("Kalıcı sil: {0}");
        page.GetElementById("profile-required")!.TextContent.ShouldBe("Önce bir profil oluşturun");
        page.QuerySelector("#deleted-purge-dialog [role='alertdialog']")!.GetAttribute("aria-modal").ShouldBe("true");
        page.GetElementById("deleted-purge-text")!.TextContent.ShouldBe("Konsept kalıcı olarak silinecek, geri alınamaz.");
        page.Body!.TextContent.ShouldNotContain("Geri yükle");

        var concepts = await WebFactory.GetDocumentAsync(client, "/", ct);
        concepts.GetElementById("concept-work")!.GetAttribute("data-text-delete").ShouldBe("Sil: {0}");
        concepts.GetElementById("concept-work")!.GetAttribute("data-text-delete-locked-video").ShouldBe("Videosu olan konsept burada silinemez");
        concepts.GetElementById("concept-delete-text")!.TextContent.ShouldBe("Konsept Silinenler'e taşınacak; geri yükleme yok.");
        concepts.QuerySelector("#concept-delete-dialog [role='alertdialog']").ShouldNotBeNull();

        var settings = await WebFactory.GetDocumentAsync(client, "/ayarlar", ct);
        settings.GetElementById("settings-suggestions-title")!.TextContent.ShouldBe("Öneriler");
        var reset = settings.GetElementById("suggestion-reset-button")!;
        reset.TextContent.ShouldBe("Önerileri sıfırla");
        reset.GetAttribute("aria-disabled").ShouldBe("true");
        settings.GetElementById("suggestion-reset-text")!.TextContent
            .ShouldBe("Seçili profilde önerilip seçmediğin başlıklar yeniden önerilebilir olacak; üretilmiş ve silinmiş konseptler etkilenmez.");
        settings.GetElementById("suggestion-reset-status")!.GetAttribute("role").ShouldBe("status");
    }

    /// <summary>AC-11/AC-12: uçlar giriş ister, POST CSRF ister; istek Api'ye iletilir ve Api'nin 404'ü taşınır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Trash_AjaxEndpoints_RequireAuthAndCsrf_Forward()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var concept = Guid.CreateVersion7();
        var profile = Guid.CreateVersion7();
        using var list = JsonDocument.Parse("""{"items":[],"page":1,"pageSize":50,"total":0}""");
        var notFound = Result<JsonElement>.Failure(new Error("concepts.not_found", "Bulunamadı.", ErrorType.NotFound));
        factory.Api.ListDeletedConceptsAsync(profile, 2, Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(list.RootElement.Clone()));
        factory.Api.GetDeletedConceptAsync(concept, Arg.Any<CancellationToken>()).Returns(notFound);
        factory.Api.SendConceptEditAsync(concept, ConceptEditCommand.Delete, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(list.RootElement.Clone()));
        factory.Api.SendConceptEditAsync(concept, ConceptEditCommand.Purge, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(notFound);
        factory.Api.SendConceptCommandAsync(ConceptCommand.ResetSuggestions, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(list.RootElement.Clone()));

        using (var anonymous = SignedInClient.CreateAnonymous(factory))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/silinenler/konseptler/{profile}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            (await anonymous.SendAsync(request, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await factory.Api.DidNotReceive().ListDeletedConceptsAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        using var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsJsonAsync($"/konseptler/{concept}/sil", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/oneriler/sifirla", new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().SendConceptEditAsync(Arg.Any<Guid>(), Arg.Any<ConceptEditCommand>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().SendConceptCommandAsync(Arg.Any<ConceptCommand>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());

        (await client.GetAsync(new Uri($"/silinenler/konseptler/{profile}?sayfa=2", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync(new Uri($"/silinenler/konseptler/detay/{concept}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAjaxAsync(client, $"/konseptler/{concept}/sil", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, $"/silinenler/konseptler/{concept}/kalici-sil", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAjaxAsync(client, "/oneriler/sifirla", new { profileId = profile }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).SendConceptEditAsync(concept, ConceptEditCommand.Delete, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendConceptEditAsync(concept, ConceptEditCommand.Purge, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendConceptCommandAsync(
            ConceptCommand.ResetSuggestions,
            Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("profileId").GetGuid() == profile),
            Arg.Any<CancellationToken>());
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
