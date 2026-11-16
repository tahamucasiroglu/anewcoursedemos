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
/// Konsept düzenleme döngüsü, Web tarafı (spec 0015 AC-15, AC-14, AC-5, AC-9): düzenleme alanı, talimat alanı ve revizyon modalı
/// etiketli ve erişilebilir; Ajax uçları giriş ister, POST'lar CSRF korumalıdır ve gövde Api'ye iletilir (alan hataları ve 404 taşınır).
/// </summary>
public sealed class ConceptsEditPageTests
{
    /// <summary>AC-15/AC-9: düzenleme ve revizyon kontrolleri, etiketler, canlı bölgeler, modal rolleri ve resx metinleri.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptsEditPage_ModalAccessible()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);

        var work = page.GetElementById("concept-work")!;
        work.GetAttribute("data-text-edit").ShouldBe("Düzenle: {0}");
        work.GetAttribute("data-text-edit-locked-queue").ShouldBe("Önce üretim sırasından çıkarın");
        work.GetAttribute("data-text-edit-locked-video").ShouldBe("Bu konseptin videosu var; metin değişikliği Üretilen Videolar'daki 'Yeniden üret' ile yapılır");
        work.GetAttribute("data-text-link-not-found").ShouldBe("Konsept bulunamadı.");
        var editor = page.GetElementById("concept-editor")!;
        editor.HasAttribute("hidden").ShouldBeTrue();
        editor.GetAttribute("data-text-saving").ShouldBe("Kaydediliyor…");
        page.GetElementById("concept-edit-save")!.TextContent.ShouldBe("Kaydet");
        page.QuerySelector("label[for='concept-instruction']")!.TextContent.ShouldBe("Ne düzeltilsin? (online modelle yeniden üretim)");
        page.GetElementById("concept-regenerate")!.TextContent.ShouldBe("Yeniden üret");
        foreach (var id in new[] { "concept-edit-status", "concept-regen-status", "revision-status" })
        {
            page.GetElementById(id)!.GetAttribute("role").ShouldBe("status", id);
        }

        foreach (var id in new[] { "concept-instruction-error", "revision-instruction-error" })
        {
            page.GetElementById(id)!.GetAttribute("role").ShouldBe("alert", id);
        }

        var dialog = page.QuerySelector("#revision-modal [role='dialog']")!;
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        page.GetElementById(dialog.GetAttribute("aria-labelledby")!)!.TextContent.ShouldBe("Konsept revizyonu");
        page.GetElementById("revision-approve")!.TextContent.ShouldBe("Onayla");
        page.GetElementById("revision-discard")!.TextContent.ShouldBe("Sil");
        page.GetElementById("revision-again")!.TextContent.ShouldBe("Tekrar düzenle");
        page.GetElementById("revision-confirm")!.GetAttribute("role").ShouldBe("alertdialog");
        page.GetElementById("revision-confirm-text")!.TextContent.ShouldBe("Revizyon silinsin mi?");
        page.GetElementById("concept-leave-text")!.TextContent.ShouldBe("Kaydedilmemiş değişiklikler kaybolacak.");
        page.QuerySelector("script[src*='main.js']").ShouldNotBeNull();
    }

    /// <summary>AC-15/AC-14: uçlar giriş ister, POST CSRF ister; gövde Api'ye iletilir; Api'nin alan hatası ve 404'ü taşınır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptsEdit_AjaxEndpoints_RequireAuthAndCsrf_Forward()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var concept = Guid.CreateVersion7();
        using var revision = JsonDocument.Parse("""{"conceptId":"c","status":"running","scenes":[]}""");
        factory.Api.GetConceptRevisionAsync(concept, Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(revision.RootElement.Clone()));
        factory.Api.SendConceptEditAsync(concept, ConceptEditCommand.Regenerate, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Failure(
            new Error("concepts.invalid", "Girilen bilgiler geçersiz.", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]> { ["Instruction"] = ["Neyin düzeltilmesini istediğinizi yazın."] },
            }));
        factory.Api.SendConceptEditAsync(concept, ConceptEditCommand.SaveScenes, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Failure(
            new Error("concepts.not_found", "Bulunamadı.", ErrorType.NotFound)));
        factory.Api.SendConceptEditAsync(concept, ConceptEditCommand.ApproveRevision, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(revision.RootElement.Clone()));
        factory.Api.SendConceptEditAsync(concept, ConceptEditCommand.DiscardRevision, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(revision.RootElement.Clone()));

        using (var anonymous = SignedInClient.CreateAnonymous(factory))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/konseptler/{concept}/revizyon");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            (await anonymous.SendAsync(request, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await factory.Api.DidNotReceive().GetConceptRevisionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        using var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsJsonAsync($"/konseptler/{concept}/yeniden-uret", new { instruction = "Kısalt" }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().SendConceptEditAsync(Arg.Any<Guid>(), Arg.Any<ConceptEditCommand>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());

        using var read = await client.GetAsync(new Uri($"/konseptler/{concept}/revizyon", UriKind.Relative), ct);
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonDocument.Parse(await read.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("status").GetString().ShouldBe("running");

        using var regenerate = await PostAjaxAsync(client, $"/konseptler/{concept}/yeniden-uret", new { instruction = " " }, ct);
        regenerate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        JsonDocument.Parse(await regenerate.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("errors").GetProperty("Instruction")[0].GetString()
            .ShouldBe("Neyin düzeltilmesini istediğinizi yazın.");
        await factory.Api.Received(1).SendConceptEditAsync(
            concept,
            ConceptEditCommand.Regenerate,
            Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("instruction").GetString() == " "),
            Arg.Any<CancellationToken>());

        using var save = await PostAjaxAsync(client, $"/konseptler/{concept}/sahneler", new { scenes = Array.Empty<object>() }, ct);
        save.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostAjaxAsync(client, $"/konseptler/{concept}/revizyon/onayla", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostAjaxAsync(client, $"/konseptler/{concept}/revizyon/sil", new { }, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).SendConceptEditAsync(concept, ConceptEditCommand.ApproveRevision, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SendConceptEditAsync(concept, ConceptEditCommand.DiscardRevision, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
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
