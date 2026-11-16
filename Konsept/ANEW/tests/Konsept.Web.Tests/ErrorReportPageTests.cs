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
/// "Hata bildir" Web tarafı (spec 0022 AC-1, AC-4, AC-14): girişli düzende etkin düğme ve erişilebilir modal (ekran görüntüsü alanı yok),
/// giriş sayfasında yok; Ajax uçları Api'ye iletir, CSRF ister, girişsiz istek girişe gider.
/// </summary>
public sealed class ErrorReportPageTests : IClassFixture<WebFactory>
{
    private readonly WebFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ErrorReportPageTests"/> class.</summary>
    /// <param name="factory">Web fabrikası.</param>
    public ErrorReportPageTests(WebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Giriş yapmışken "Hata bildir" etkin bir düğmedir ve etiketli bir diyalog açar (açıklama alanı, otomatik bilgiler, İptal/Gönder; dosya
    /// alanı yok); giriş sayfasında ne düğme ne modal vardır.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Layout_SignedIn_ReportButtonAndModal_LoginHasNone()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = await _factory.CreateSignedInClientAsync(ct);
        using var anonymous = SignedInClient.CreateAnonymous(_factory);

        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var login = await WebFactory.GetDocumentAsync(anonymous, "/giris", ct);

        var button = page.QuerySelector("button#report-open")!;
        button.ClassList.ShouldNotContain("passive");
        button.HasAttribute("aria-disabled").ShouldBeFalse();
        button.GetAttribute("aria-label").ShouldBe("Hata bildir");
        button.GetAttribute("aria-controls").ShouldBe("report-modal");
        var dialog = page.QuerySelector("#report-modal [role='dialog']")!;
        dialog.GetAttribute("aria-modal").ShouldBe("true");
        page.QuerySelector("#report-modal-title")!.TextContent.ShouldBe("Hata bildir");
        page.QuerySelector("label[for='report-description']").ShouldNotBeNull();
        page.QuerySelector("#report-description")!.GetAttribute("aria-describedby").ShouldBe("report-description-hint report-description-error");
        page.QuerySelector("#report-auto-title")!.TextContent.ShouldBe("Bildirime otomatik eklenecek bilgiler");
        page.QuerySelector("#report-cancel")!.TextContent.ShouldBe("İptal");
        page.QuerySelector("#report-submit")!.TextContent.ShouldBe("Gönder");
        page.QuerySelectorAll("#report-modal input[type='file']").ShouldBeEmpty();
        login.QuerySelector("#report-open").ShouldBeNull();
        login.QuerySelector("#report-modal").ShouldBeNull();
    }

    /// <summary>
    /// <c>/hata-bildir</c> açıklama, sayfa ve korelasyonu Api'ye iletir ve numarayı döner; doğrulama hatası alanıyla, sınır 429 mesajıyla
    /// döner; CSRF'siz POST reddedilir; önizleme iletilir; girişsiz istek girişe gider.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ErrorReportAjax_ForwardsAndAnonymousRedirects()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        var page = await WebFactory.GetDocumentAsync(client, "/", ct);
        var token = page.QuerySelector("meta[name='csrf-token']")!.GetAttribute("content")!;
        factory.Api.SubmitErrorReportAsync(Arg.Is<ErrorReportApiRequest?>(r => r!.Description == "Video oluşmadı, ekran donuyor"), Arg.Any<CancellationToken>())
            .Returns(Result<ErrorReportReceiptResponse>.Success(new ErrorReportReceiptResponse(42)));
        factory.Api.SubmitErrorReportAsync(Arg.Is<ErrorReportApiRequest?>(r => r!.Description == "kısa"), Arg.Any<CancellationToken>())
            .Returns(Result<ErrorReportReceiptResponse>.Failure(new Error("error_report.description", "Açıklama en az 10 karakter olmalı.", ErrorType.Validation)
            {
                FieldErrors = new Dictionary<string, string[]> { ["description"] = ["Açıklama en az 10 karakter olmalı."] },
            }));
        factory.Api.SubmitErrorReportAsync(Arg.Is<ErrorReportApiRequest?>(r => r!.Description == "on birinci bildirim"), Arg.Any<CancellationToken>())
            .Returns(Result<ErrorReportReceiptResponse>.Failure(new Error("error_report.rate_limited", "Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin", ErrorType.TooManyRequests)));
        factory.Api.GetErrorReportPreviewAsync(Arg.Any<CancellationToken>())
            .Returns(Result<ErrorReportPreviewResponse>.Success(new ErrorReportPreviewResponse("1.2.3", DateTimeOffset.UnixEpoch, [new SystemEventResponse("t", 0, "Giriş yapıldı", DateTimeOffset.UnixEpoch)])));

        var ok = await PostAsync(client, new { description = "Video oluşmadı, ekran donuyor", page = "/uretilenler", correlationId = "corr-1" }, token, ct);
        var invalid = await PostAsync(client, new { description = "kısa" }, token, ct);
        var limited = await PostAsync(client, new { description = "on birinci bildirim" }, token, ct);
        var noCsrf = await PostAsync(client, new { description = "Video oluşmadı, ekran donuyor" }, null, ct);
        var preview = await client.GetFromJsonAsync<JsonElement>("/hata-bildir/onizleme", ct);
        using var anonymous = SignedInClient.CreateAnonymous(factory);
        using var redirect = await anonymous.GetAsync(new Uri("/hata-bildir/onizleme", UriKind.Relative), ct);

        ok.Status.ShouldBe(HttpStatusCode.OK);
        ok.Body.GetProperty("number").GetInt64().ShouldBe(42);
        await factory.Api.Received().SubmitErrorReportAsync(
            Arg.Is<ErrorReportApiRequest?>(r => r!.Page == "/uretilenler" && r.CorrelationId == "corr-1"),
            Arg.Any<CancellationToken>());
        invalid.Status.ShouldBe(HttpStatusCode.BadRequest);
        invalid.Body.GetProperty("errors").GetProperty("description")[0].GetString().ShouldBe("Açıklama en az 10 karakter olmalı.");
        limited.Status.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Body.GetProperty("message").GetString().ShouldBe("Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin");
        noCsrf.Status.ShouldBe(HttpStatusCode.BadRequest);
        preview.GetProperty("version").GetString().ShouldBe("1.2.3");
        preview.GetProperty("journal")[0].GetProperty("message").GetString().ShouldBe("Giriş yapıldı");
        redirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        redirect.Headers.Location!.AbsolutePath.ShouldBe("/giris");
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(HttpClient client, object body, string? token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hata-bildir") { Content = JsonContent.Create(body) };
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        if (token is not null)
        {
            request.Headers.Add("RequestVerificationToken", token);
        }

        using var response = await client.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
