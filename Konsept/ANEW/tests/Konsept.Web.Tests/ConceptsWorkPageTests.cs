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
/// Konseptler'in dört alanı, Web tarafı (spec 0014 AC-29, AC-8, AC-24): alanlar ve kontroller etiketli ve klavyeyle erişilebilir, ilerleme
/// ve hata alanları ekran okuyucuya duyurulur, varsayılan filtre Üretildi + Onaylandı; Ajax uçları giriş ister, POST'lar CSRF korumalıdır
/// ve gövde Api'ye iletilir (alan hataları ve sade mesajlar taşınır).
/// </summary>
public sealed class ConceptsWorkPageTests
{
    /// <summary>AC-29/AC-24: dört alanın kontrolleri, etiketleri, canlı bölgeleri ve filtre varsayılanı.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptsWorkPage_AreasAccessible_AjaxEndpointsCsrf()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);

        page.GetElementById("concept-work")!.HasAttribute("data-requires-profile").ShouldBeTrue();
        page.QuerySelector("label[for='topic-count']")!.TextContent.ShouldBe("Öneri adedi");
        page.GetElementById("topic-count")!.GetAttribute("type").ShouldBe("number");
        page.GetElementById("topic-generate")!.TextContent.ShouldBe("Konu üret");
        page.GetElementById("topic-produce")!.HasAttribute("disabled").ShouldBeTrue();
        page.QuerySelector("label[for='idea-text']")!.TextContent.ShouldBe("Fikriniz (en fazla 2.000 karakter)");
        page.QuerySelector("label[for='idea-count']")!.TextContent.ShouldBe("Konsept adedi");
        foreach (var id in new[] { "topic-progress", "idea-progress", "concepts-status" })
        {
            page.GetElementById(id)!.GetAttribute("role").ShouldBe("status", id);
        }

        foreach (var id in new[] { "topic-count-error", "idea-text-error", "idea-count-error" })
        {
            page.GetElementById(id)!.GetAttribute("role").ShouldBe("alert", id);
        }

        page.GetElementById("topic-stop")!.TextContent.ShouldBe("Durdur");
        page.QuerySelectorAll("[data-not-ready]").Select(n => n.GetAttribute("data-not-ready")).ShouldBe(["topics", "fromTopics", "idea"]);
        page.QuerySelectorAll("[data-not-ready] a").ShouldAllBe(a => a.GetAttribute("href") == "/ayarlar");
        var filters = page.GetElementById("concept-filter")!.QuerySelectorAll("input");
        filters.Select(f => f.GetAttribute("value")).ShouldBe(["generated", "approved", "queued", "producing", "video_ready"]);
        filters.Select(f => f.HasAttribute("checked")).ShouldBe([true, true, false, false, false]);
        page.GetElementById("concepts-approve-selected")!.HasAttribute("disabled").ShouldBeTrue();
        page.GetElementById("concept-detail-hint")!.TextContent.ShouldBe("Okumak için Üretilenler'den bir konsept seçin.");
        page.GetElementById("concept-work")!.GetAttribute("data-text-approve").ShouldBe("Onayla: {0}");
        page.GetElementById("concept-work")!.GetAttribute("data-text-unapprove").ShouldBe("Onayı geri al: {0}");
    }

    /// <summary>AC-29/AC-28: uçlar giriş ister, POST CSRF ister; gövde Api'ye iletilir; Api'nin alan hatası ve 404'ü taşınır.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptsWork_AjaxEndpoints_RequireAuthAndCsrf_Forward()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var profile = Guid.CreateVersion7();
        var concept = Guid.CreateVersion7();
        using var work = JsonDocument.Parse("""{"profileId":"p","readiness":{"topics":true},"suggestions":[]}""");
        using var detail = JsonDocument.Parse("""{"id":"c","title":"<b>Başlık</b>","scenes":[]}""");
        factory.Api.GetConceptWorkAsync(profile, Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(work.RootElement.Clone()));
        factory.Api.GetConceptAsync(concept, Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Failure(new Error("concepts.not_found", "Bulunamadı.", ErrorType.NotFound)));
        factory.Api.ListConceptsAsync(profile, "approved", 2, Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(detail.RootElement.Clone()));
        factory.Api.SendConceptCommandAsync(ConceptCommand.Topics, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Failure(
            new Error("concepts.invalid", "Girilen bilgiler geçersiz.", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]> { ["Count"] = ["1 ile 100 arasında bir tam sayı girin."] },
            }));
        factory.Api.SendConceptCommandAsync(ConceptCommand.Approve, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>()).Returns(Result<JsonElement>.Success(work.RootElement.Clone()));

        using (var anonymous = SignedInClient.CreateAnonymous(factory))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/konsept-uretim/{profile}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            (await anonymous.SendAsync(request, ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await factory.Api.DidNotReceive().GetConceptWorkAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        using var client = await factory.CreateSignedInClientAsync(ct);
        (await client.PostAsJsonAsync("/konsept-uretim/konu", new { profileId = profile, count = 5 }, ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().SendConceptCommandAsync(Arg.Any<ConceptCommand>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());

        using var read = await client.GetAsync(new Uri($"/konsept-uretim/{profile}", UriKind.Relative), ct);
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonDocument.Parse(await read.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("readiness").GetProperty("topics").GetBoolean().ShouldBeTrue();
        (await client.GetAsync(new Uri($"/konseptler/detay/{concept}", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync(new Uri($"/konseptler/liste/{profile}?durumlar=approved&sayfa=2", UriKind.Relative), ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var start = await PostAjaxAsync(client, "/konsept-uretim/konu", new { profileId = profile, count = 0 }, ct);
        start.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        JsonDocument.Parse(await start.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("errors").GetProperty("Count")[0].GetString()
            .ShouldBe("1 ile 100 arasında bir tam sayı girin.");
        await factory.Api.Received(1).SendConceptCommandAsync(
            ConceptCommand.Topics,
            Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("count").GetInt32() == 0),
            Arg.Any<CancellationToken>());

        using var approve = await PostAjaxAsync(client, "/konseptler/onayla", new { ids = new[] { concept } }, ct);
        approve.StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).SendConceptCommandAsync(ConceptCommand.Approve, Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
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
