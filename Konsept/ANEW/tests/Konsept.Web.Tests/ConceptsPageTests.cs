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
/// Konseptler sayfası, Web tarafı (spec 0005 AC-1, AC-5, AC-7, AC-9): şerit (model bilgisi, ince ayar, kelime denetimi) ve dört alan
/// tasarımdaki sırayla sunucuda gelir; model modalı ayrı, erişilebilir bir pencere; seçim kontrollerinin etiketi rol adını içerir;
/// ince ayarda 8/4/4 seçenek + "Belirtilmedi"; profil yokken + yönlendirmesi; Ajax uçları giriş ister, CSRF korumalıdır ve Api'ye iletir.
/// </summary>
public sealed class ConceptsPageTests
{
    /// <summary>AC-1/AC-9: şerit sırası, rol özetleri "seçilmedi", kelime denetimi kutusu kapalı, dört alan başlıkları ve modal erişilebilirliği.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptsPage_StripAndFourAreas()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var strip = page.GetElementById("concept-strip")!;

        strip.HasAttribute("data-requires-profile").ShouldBeTrue();
        strip.GetAttribute("data-url").ShouldBe("/konsept-ayarlari");
        strip.Children.Take(3).Select(c => c.ClassName).ShouldBe(["concept-strip__models", "concept-strip__finetune", "concept-strip__wordcheck"]);
        strip.QuerySelectorAll("[data-role]").Select(r => r.GetAttribute("data-role")).ShouldBe(["concept", "dedupe", "topic"]);
        strip.QuerySelectorAll("[data-role-value]").Select(v => v.TextContent).ShouldBe(["seçilmedi", "seçilmedi", "seçilmedi"]);
        strip.QuerySelector("[data-role='concept']")!.TextContent.ShouldStartWith("Konsept:");
        strip.QuerySelectorAll("[data-group-value]").Select(v => v.TextContent).ShouldBe(["Belirtilmedi", "Belirtilmedi", "Belirtilmedi"]);
        var wordCheck = page.GetElementById("concept-wordcheck")!;
        wordCheck.GetAttribute("type").ShouldBe("checkbox");
        wordCheck.HasAttribute("checked").ShouldBeFalse();
        wordCheck.ParentElement!.TextContent.Trim().ShouldBe("Kelime denetimi");
        var modelsButton = page.GetElementById("concept-models-open")!;
        modelsButton.GetAttribute("aria-haspopup").ShouldBe("dialog");
        modelsButton.HasAttribute("aria-label").ShouldBeFalse(); // Erişilebilir ad görünen özetten gelir (review r1 F-3).
        modelsButton.TextContent.ShouldContain("Konsept: seçilmedi");
        page.GetElementById("concept-finetune-toggle")!.GetAttribute("aria-expanded").ShouldBe("false");

        var areas = page.QuerySelector(".concept-areas")!;
        areas.HasAttribute("data-requires-profile").ShouldBeTrue();
        areas.QuerySelectorAll(".concept-area__title").Select(h => h.TextContent).ShouldBe(["Konudan üret", "Fikirden üret", "Üretilenler", "Düzenleme alanı"]);

        var modal = page.GetElementById("concept-models-modal")!;
        modal.HasAttribute("hidden").ShouldBeTrue();
        var dialog = modal.QuerySelector("[role='dialog']")!;
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        page.GetElementById(dialog.GetAttribute("aria-labelledby")!)!.TextContent.ShouldBe("Model bilgisi");
        page.GetElementById("concept-models-close")!.GetAttribute("aria-label").ShouldBe("Kapat ve kaydet");
        string[] modelLabels = ["Konsept üretimi modeli", "Anlamca tekrar denetimi modeli", "Konu önerisi modeli"];
        string[] effortLabels = ["Konsept üretimi eforu", "Anlamca tekrar denetimi eforu", "Konu önerisi eforu"];
        modal.QuerySelectorAll("[data-model-select]").Select(s => page.QuerySelector($"label[for='{s.Id}']")!.TextContent).ShouldBe(modelLabels);
        modal.QuerySelectorAll("[data-effort-select]").Select(s => page.QuerySelector($"label[for='{s.Id}']")!.TextContent).ShouldBe(effortLabels);
        page.GetElementById("concept-models-empty")!.QuerySelector("a")!.GetAttribute("href").ShouldBe("/ayarlar");
    }

    /// <summary>AC-5: ince ayar alanı tonda 8, izleyicide 4, anlatım biçiminde 4 seçenek ve her grupta "Belirtilmedi" (radyo: grupta tek seçim).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptsPage_FineTuneOptions()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var panel = page.GetElementById("concept-finetune")!;

        panel.HasAttribute("hidden").ShouldBeTrue();
        panel.QuerySelectorAll("legend").Select(l => l.TextContent).ShouldBe(["Ton", "İzleyici", "Anlatım biçimi"]);
        foreach (var (group, count) in new[] { ("tone", 8), ("audience", 4), ("narration", 4) })
        {
            var radios = panel.QuerySelectorAll($"input[name='{group}']").ToList();
            radios.ShouldAllBe(r => r.GetAttribute("type") == "radio");
            radios.Count.ShouldBe(count + 1, group);
            radios[0].GetAttribute("value").ShouldBe(string.Empty);
            radios[0].ParentElement!.TextContent.Trim().ShouldBe("Belirtilmedi");
            radios.Count(r => r.HasAttribute("checked")).ShouldBe(1, group);
        }

        panel.QuerySelectorAll("input[name='tone']").Skip(1).Select(r => r.ParentElement!.TextContent.Trim()).ShouldBe(
            ["Gizemli", "Eğitici", "Dramatik", "Sakin", "Belgesel", "Esprili", "İlham verici", "Sorgulayıcı"]);
        panel.QuerySelectorAll("input[name='audience']").Skip(1).Select(r => r.ParentElement!.TextContent.Trim()).ShouldBe(
            ["Konuya yeni başlayan", "Meraklı", "Aile ve çocuklara uygun", "Konuyu bilen"]);
        panel.QuerySelectorAll("input[name='narration']").Skip(1).Select(r => r.ParentElement!.TextContent.Trim()).ShouldBe(
            ["Tek bir hikâye", "Sıralı liste", "Efsane mi gerçek mi", "Ya olsaydı"]);
    }

    /// <summary>AC-7: profil yokken görünen + yönlendirmesi sunucuda gizli gelir (profiles.js gösterir); "Önce bir profil oluşturun" durumu düzendedir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptsPage_NoProfileHint()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var hint = page.QuerySelector("[data-requires-no-profile]")!;

        hint.HasAttribute("hidden").ShouldBeTrue();
        hint.TextContent.ShouldContain("üst bardaki + düğmesiyle bir profil ekleyin");
        page.GetElementById("concepts-add-profile")!.TextContent.ShouldBe("Profil ekle");
        page.GetElementById("profile-required")!.TextContent.ShouldBe("Önce bir profil oluşturun");
    }

    /// <summary>AC-8/AC-9: uçlar giriş ister (Api çağrılmaz), POST CSRF ister; okuma ve kaydetme Api'ye aynen iletilir; Api'nin hataları (404, alan hatası) döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ConceptsPage_AjaxEndpoints_RequireAuthAndCsrf_Forward()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        var id = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        var settings = new ConceptSettingsResponse(
            id,
            [new RoleModelResponse("concept", "anthropic/claude-opus-4-5", "Claude Opus 4.5", "high")],
            "calm",
            null,
            null,
            true,
            [new SelectableModelResponse("anthropic/claude-opus-4-5", "Claude Opus 4.5", ["low", "medium", "high"], "high")]);
        factory.Api.GetConceptSettingsAsync(id, Arg.Any<CancellationToken>()).Returns(Result<ConceptSettingsResponse>.Success(settings));
        factory.Api.GetConceptSettingsAsync(other, Arg.Any<CancellationToken>()).Returns(Result<ConceptSettingsResponse>.Failure(
            new Error("profiles.not_found", "Profil bulunamadı.", ErrorType.NotFound)));
        factory.Api.SaveConceptSettingsAsync(id, Arg.Any<SaveConceptSettingsApiRequest?>(), Arg.Any<CancellationToken>()).Returns(Result<ConceptSettingsResponse>.Failure(
            new Error("profiles.invalid", "Girilen bilgiler geçersiz.", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]> { ["Models"] = ["Bu model seçilemez: kullanma yetkiniz ya da geçerli bir API key'iniz yok."] },
            }));

        using (var anonymous = SignedInClient.CreateAnonymous(factory))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/konsept-ayarlari/{id}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var denied = await anonymous.SendAsync(request, ct);
            denied.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            await factory.Api.DidNotReceive().GetConceptSettingsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        using var client = await factory.CreateSignedInClientAsync(ct);
        var body = new { models = new[] { new { role = "concept", modelId = "openai/gpt-5", effort = (string?)null } } };
        using var withoutToken = await client.PostAsJsonAsync($"/konsept-ayarlari/{id}", body, ct);
        withoutToken.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.DidNotReceive().SaveConceptSettingsAsync(Arg.Any<Guid>(), Arg.Any<SaveConceptSettingsApiRequest?>(), Arg.Any<CancellationToken>());

        using var read = await client.GetAsync(new Uri($"/konsept-ayarlari/{id}", UriKind.Relative), ct);
        using var notFound = await client.GetAsync(new Uri($"/konsept-ayarlari/{other}", UriKind.Relative), ct);
        using var save = await PostAjaxAsync(client, $"/konsept-ayarlari/{id}", body, ct);

        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await read.Content.ReadAsStringAsync(ct)).RootElement;
        json.GetProperty("roles")[0].GetProperty("modelName").GetString().ShouldBe("Claude Opus 4.5");
        json.GetProperty("wordCheck").GetBoolean().ShouldBeTrue();
        notFound.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        save.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        JsonDocument.Parse(await save.Content.ReadAsStringAsync(ct)).RootElement.GetProperty("errors").GetProperty("Models")[0].GetString()
            .ShouldBe("Bu model seçilemez: kullanma yetkiniz ya da geçerli bir API key'iniz yok.");
        await factory.Api.Received(1).SaveConceptSettingsAsync(
            id,
            Arg.Is<SaveConceptSettingsApiRequest?>(r => r != null && r.Models!.Count == 1 && r.Models[0].ModelId == "openai/gpt-5" && r.FineTune == null && r.WordCheck == null),
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
