using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Admin Hata bildirimleri, Web tarafı (spec 0025 AC-1, AC-2, AC-8): sekme en sonda, filtreler etiketli ve varsayılan Açık, tablo
/// başlıkları, detay penceresi; Ajax uçları Api'ye parametreleri ve gövdeyi iletir (antiforgery ile); yetkisiz kullanıcı 403 ve
/// Api'ye gidilmez.
/// </summary>
public sealed class AdminErrorReportsPageTests
{
    /// <summary>
    /// Panel: Hata bildirimleri sekmesi en sonda ve gizli panelle; kaynak (Tümü/Web/Ajan/API) ve durum (Açık varsayılan) filtreleri;
    /// tablo sütunları; detay penceresi modal, durum seçimi dört durum, not alanı etiketli ve sınır uyarısı; silme düğmesi yok.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminPanel_ReportsTab_MarkupAccessible()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateAdminClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/admin", ct);

        page.QuerySelectorAll("#admin-panel [role='tab']").Select(t => t.TextContent.Trim()).Last().ShouldBe("Hata bildirimleri");
        page.QuerySelector("#admin-reports")!.HasAttribute("hidden").ShouldBeTrue();
        page.QuerySelector("#admin-reports")!.GetAttribute("data-reports-url").ShouldBe("/admin/hata-bildirimleri");
        page.QuerySelector("#admin-reports")!.GetAttribute("data-text-empty").ShouldBe("Açık hata bildirimi yok");
        page.QuerySelector("#admin-reports")!.GetAttribute("data-text-no-match").ShouldBe("Filtreye uyan bildirim yok");
        page.QuerySelectorAll("#admin-report-source-filter option").Select(o => o.TextContent).ShouldBe(["Tümü", "Web", "Ajan", "API"]);
        page.QuerySelectorAll("#admin-report-status-filter option").Select(o => o.TextContent)
            .ShouldBe(["Açık", "Yeni", "İnceleniyor", "Çözüldü", "Yok sayıldı", "Tümü"]);
        page.QuerySelector("#admin-report-status-filter option[selected]")!.GetAttribute("value").ShouldBe("open");
        page.QuerySelectorAll("#admin-report-status option").Select(o => o.GetAttribute("value")).ShouldBe(["new", "reviewing", "resolved", "ignored"]);
        foreach (var id in new[] { "admin-report-source-filter", "admin-report-status-filter", "admin-report-status", "admin-report-note" })
        {
            page.QuerySelector($"label[for='{id}']").ShouldNotBeNull(id);
        }

        page.QuerySelectorAll("#admin-report-table th[scope='col']").Select(t => t.TextContent)
            .ShouldBe(["Zaman", "Kaynak", "Bildiren", "Mesaj", "Durum", "İşlem"]);
        page.QuerySelector("#admin-reports-status")!.GetAttribute("role").ShouldBe("status");
        page.QuerySelector("#admin-reports-error")!.GetAttribute("role").ShouldBe("alert");
        page.QuerySelector("#admin-report-dialog [role='dialog']")!.GetAttribute("aria-modal").ShouldBe("true");
        page.QuerySelector("#admin-report-note-error")!.TextContent.ShouldBe("En fazla 500 karakter");
        page.QuerySelector("#admin-report-save")!.TagName.ShouldBe("BUTTON");
        page.QuerySelector("#admin-report-description")!.TagName.ShouldBe("PRE");
        page.QuerySelector("#admin-report-dialog")!.TextContent.ShouldNotContain("Sil");
    }

    /// <summary>Ajax uçları: liste sayfa/kaynak/durum, detay numarası ve durum gövdesi Api'ye iletilir; yanıt geçer (AC-2, AC-6).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminReportsAjax_ForwardsToApi()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        factory.Api.GetAdminErrorReportsAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(JsonDocument.Parse("""{"total":3}""").RootElement));
        factory.Api.GetAdminErrorReportAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(JsonDocument.Parse("""{"report":{"number":7}}""").RootElement));
        factory.Api.SetErrorReportStatusAsync(Arg.Any<long>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>())
            .Returns(Result<JsonElement>.Success(JsonDocument.Parse("""{"report":{"status":"resolved"}}""").RootElement));
        using var client = await factory.CreateAdminClientAsync(ct);

        using var list = await GetAjaxAsync(client, "/admin/hata-bildirimleri?sayfa=2&kaynak=agent&durum=resolved", ct);
        using var detail = await GetAjaxAsync(client, "/admin/hata-bildirimleri/7", ct);
        using var status = await PostAjaxAsync(client, "/admin/hata-bildirimleri/7/durum", new { status = "resolved", note = "tamam" }, ct);

        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await list.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("total").GetInt32().ShouldBe(3);
        detail.StatusCode.ShouldBe(HttpStatusCode.OK);
        status.StatusCode.ShouldBe(HttpStatusCode.OK);
        await factory.Api.Received(1).GetAdminErrorReportsAsync(2, "agent", "resolved", Arg.Any<CancellationToken>());
        await factory.Api.Received(1).GetAdminErrorReportAsync(7, Arg.Any<CancellationToken>());
        await factory.Api.Received(1).SetErrorReportStatusAsync(
            7, Arg.Is<JsonElement?>(b => b.HasValue && b.Value.GetProperty("status").GetString() == "resolved" && b.Value.GetProperty("note").GetString() == "tamam"), Arg.Any<CancellationToken>());
    }

    /// <summary>Yetkisi olmayan kullanıcı: liste, detay ve durum Ajax'ı 403 "Bu işlem için yetkiniz yok"; Api'ye istek gitmez (AC-1).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AdminReportsAjax_UserRole_Forbidden_NoApiCall()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var user = await factory.CreateSignedInClientAsync(ct);

        using var list = await GetAjaxAsync(user, "/admin/hata-bildirimleri?sayfa=1", ct);
        using var detail = await GetAjaxAsync(user, "/admin/hata-bildirimleri/1", ct);
        using var status = await PostAjaxAsync(user, "/admin/hata-bildirimleri/1/durum", new { status = "resolved" }, ct);

        foreach (var response in new[] { list, detail, status })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("message").GetString().ShouldBe("Bu işlem için yetkiniz yok");
        }

        await factory.Api.DidNotReceive().GetAdminErrorReportsAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().GetAdminErrorReportAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await factory.Api.DidNotReceive().SetErrorReportStatusAsync(Arg.Any<long>(), Arg.Any<JsonElement?>(), Arg.Any<CancellationToken>());
    }

    private static Task<HttpResponseMessage> GetAjaxAsync(HttpClient client, string path, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Accept.ParseAdd("application/json");
        return client.SendAsync(request, ct);
    }

    private static async Task<HttpResponseMessage> PostAjaxAsync(HttpClient client, string path, object body, CancellationToken ct)
    {
        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var token = page.QuerySelector("meta[name='csrf-token']")!.GetAttribute("content")!;
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("RequestVerificationToken", token);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Accept.ParseAdd("application/json");
        return await client.SendAsync(request, ct);
    }
}
