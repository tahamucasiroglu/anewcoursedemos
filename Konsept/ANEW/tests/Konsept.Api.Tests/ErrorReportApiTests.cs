using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Api.Tests.Fakes;
using Konsept.Api.Tests.Support;
using Konsept.Modules.Monitoring.Application.Abstract;
using Konsept.Modules.Monitoring.Domain.Entities;
using Konsept.Modules.Monitoring.Domain.Enums;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;

namespace Konsept.Api.Tests;

/// <summary>
/// Web hata bildirimi ve API'nin otomatik kaydı (spec 0022 AC-1, AC-3, AC-4, AC-12, AC-13, AC-15): bağlam ve maskeleme, açıklama sınırları,
/// saatlik sınır (web + ajan; API sayılmaz), beklenmeyen hatada tek kayıt, 4xx'te kayıt yok.
/// </summary>
public sealed class ErrorReportApiTests : IClassFixture<FastApiFactory>
{
    private const string Path = "/api/error-reports";
    private const string Jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ0ZXN0In0.c2lnbmF0dXJlLXRlc3Q";
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(10);
    private readonly FastApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="ErrorReportApiTests"/> class.</summary>
    /// <param name="factory">Hızlı API fabrikası.</param>
    public ErrorReportApiTests(FastApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Önizleme versiyonu ve son 20 Günlük kaydını döner; gönderim numara döner ve kayıt gönderen kullanıcı, kaynak web, "Yeni", versiyon,
    /// sayfa, korelasyon numarası ve son 20 Günlük kaydıyla saklanır; açıklamadaki/eklerdeki key ve JWT maskelidir; Günlük'e Bilgi
    /// "Hata bildirimi gönderildi" düşer.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ErrorReport_Web_StoredWithContextMaskedAndJournal()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var userId = _factory.Users.Get(email).Id;
        var start = DateTimeOffset.UtcNow.AddMinutes(-30);
        for (var i = 0; i < 25; i++)
        {
            var text = i == 24 ? "son kayıt sk-proj-JOURNALKEY12345-j1k" : $"kayıt {i:00}";
            await _factory.Events.AddAsync(SystemEvent.Create("t", EventLevel.Info, text, start.AddSeconds(i), userId), ct);
        }

        var preview = await client.GetFromJsonAsync<JsonElement>(Path + "/preview", ct);
        using var response = await client.PostAsJsonAsync(
            Path,
            new { description = $"Video üretilmedi, anahtarım sk-proj-DESCKEY1234567-d2k ve token {Jwt}", page = "/uretilenler", correlationId = "corr-0022-a" },
            ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        preview.GetProperty("version").GetString().ShouldNotBeNullOrWhiteSpace();
        preview.GetProperty("journal").GetArrayLength().ShouldBe(20);
        preview.ToString().ShouldNotContain("JOURNALKEY12345");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var number = body.GetProperty("number").GetInt64();
        var report = _factory.ErrorReports.Reports.Single(r => r.Number == number);
        report.UserId.ShouldBe(userId);
        report.Source.ShouldBe(ErrorReportSource.Web);
        report.Status.ShouldBe(ErrorReportStatus.New);
        report.ProductVersion.ShouldBe(preview.GetProperty("version").GetString());
        report.Page.ShouldBe("/uretilenler");
        report.CorrelationId.ShouldBe("corr-0022-a");
        report.Attachments!.Split('\n').Length.ShouldBe(20);
        report.Attachments.ShouldContain("son kayıt");
        report.Attachments.ShouldNotContain("kayıt 04"); // en yeni 20: 05–24
        var stored = report.Description + report.Attachments;
        stored.ShouldNotContain("DESCKEY1234567");
        stored.ShouldNotContain("JOURNALKEY12345");
        stored.ShouldNotContain(Jwt);
        report.Description!.ShouldContain("Video üretilmedi");
        _factory.Events.Events.ShouldContain(e => e.UserId == userId && e.Type == "error_report.sent" && e.Level == EventLevel.Info && e.Message == "Hata bildirimi gönderildi");
    }

    /// <summary>Boş, 9 ve 4001 karakterlik açıklama Türkçe alan mesajıyla 400 alır ve kayıt oluşturmaz; 10 ve 4000 karakter kabul edilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ErrorReport_Web_DescriptionBounds()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var userId = _factory.Users.Get(email).Id;

        var rejected = new List<(HttpStatusCode Status, string Message)>();
        foreach (var text in new[] { string.Empty, new string('a', 9), new string('a', 4001) })
        {
            using var response = await client.PostAsJsonAsync(Path, new { description = text, page = "/" }, ct);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            rejected.Add((response.StatusCode, problem.GetProperty("errors").GetProperty("description")[0].GetString()!));
        }

        var countAfterRejected = _factory.ErrorReports.Reports.Count(r => r.UserId == userId);
        using var ten = await client.PostAsJsonAsync(Path, new { description = new string('b', 10), page = "/" }, ct);
        using var max = await client.PostAsJsonAsync(Path, new { description = new string('c', 4000), page = "/" }, ct);

        rejected.ShouldAllBe(r => r.Status == HttpStatusCode.BadRequest);
        rejected[0].Message.ShouldBe("Lütfen karşılaştığınız sorunu yazın.");
        rejected[1].Message.ShouldBe("Açıklama en az 10 karakter olmalı.");
        rejected[2].Message.ShouldBe("Açıklama en fazla 4000 karakter olabilir.");
        countAfterRejected.ShouldBe(0);
        ten.StatusCode.ShouldBe(HttpStatusCode.OK);
        max.StatusCode.ShouldBe(HttpStatusCode.OK);
        _factory.ErrorReports.Reports.Count(r => r.UserId == userId).ShouldBe(2);
    }

    /// <summary>
    /// Bir saatte 10. bildirim (3 ajan + 7 web) kabul edilir, 11.'si "Çok fazla bildirim…" ile 429 alır ve kayıt oluşmaz; API'nin otomatik
    /// kayıtları ve bir saatten eski bildirimler sayılmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ErrorReport_RateLimit_TenPerHourWebPlusAgent()
    {
        var ct = TestContext.Current.CancellationToken;
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(_factory, ct, email);
        var userId = _factory.Users.Get(email).Id;
        var now = DateTimeOffset.UtcNow;
        await _factory.ErrorReports.AddAsync(ErrorReport.CreateApi(userId, now, "1", "api hatası", "c"), ct);
        await _factory.ErrorReports.AddAsync(ErrorReport.CreateApi(userId, now, "1", "api hatası", "c"), ct);
        await _factory.ErrorReports.AddAsync(ErrorReport.CreateWeb(userId, now.AddMinutes(-61), "1", "eski bildirim metni", "/", null, []), ct);
        using var scope = _factory.Services.CreateScope();
        var submitter = scope.ServiceProvider.GetRequiredService<IErrorReportService>();
        for (var i = 0; i < 3; i++)
        {
            (await submitter.SubmitFromAgentAsync(userId, new("ajan hatası", "1", "Windows", null, null, []), ct)).Outcome
                .ShouldBe(Modules.Monitoring.Contracts.Enums.ErrorReportOutcome.Accepted);
        }

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 7; i++)
        {
            using var ok = await client.PostAsJsonAsync(Path, new { description = $"web bildirimi {i}", page = "/" }, ct);
            statuses.Add(ok.StatusCode);
        }

        var before = _factory.ErrorReports.Reports.Count(r => r.UserId == userId);
        using var eleventh = await client.PostAsJsonAsync(Path, new { description = "on birinci bildirim", page = "/" }, ct);
        var problem = await eleventh.Content.ReadFromJsonAsync<JsonElement>(ct);
        var agentEleventh = await submitter.SubmitFromAgentAsync(userId, new("ajan hatası", "1", "Windows", null, null, []), ct);

        statuses.ShouldAllBe(s => s == HttpStatusCode.OK);
        eleventh.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        problem.GetProperty("title").GetString().ShouldBe("Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin");
        agentEleventh.Outcome.ShouldBe(Modules.Monitoring.Contracts.Enums.ErrorReportOutcome.RateLimited);
        _factory.ErrorReports.Reports.Count(r => r.UserId == userId).ShouldBe(before);
    }

    /// <summary>
    /// API'de beklenmeyen hata (500) kaynak "API", "Yeni", korelasyon, versiyon, maskeli özet ve oturumdaki kullanıcıyla tek kayıt oluşturur;
    /// doğrulama (400), yetkisiz (401/403) ve bulunamadı (404) yanıtları kayıt oluşturmaz.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task ErrorReport_UnexpectedApiError_RecordedOnce_4xxNot()
    {
        var ct = TestContext.Current.CancellationToken;
        var failing = _factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.RemoveAll<ISystemStatusService>();
            s.AddScoped<ISystemStatusService, ThrowingSystemStatusService>();
        }));
        var email = AuthenticatedClient.NewEmail();
        using var client = await AuthenticatedClient.CreateAsync(failing, ct, email);
        var userId = _factory.Users.Get(email).Id;
        var apiBefore = _factory.ErrorReports.Reports.Count(r => r.Source == ErrorReportSource.Api);

        using var response = await client.GetAsync(new Uri("/api/system/status", UriKind.Relative), ct);
        var correlationId = (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("correlationId").GetString();
        using var invalid = await client.PostAsJsonAsync(Path, new { description = "kısa" }, ct);
        using var forbidden = await client.GetAsync(new Uri(Path + "/1", UriKind.Relative), ct);
        using var notFound = await client.GetAsync(new Uri("/api/profiles/" + Guid.NewGuid(), UriKind.Relative), ct);
        using var anonymous = failing.CreateClient();
        using var unauthorized = await anonymous.GetAsync(new Uri("/api/journal", UriKind.Relative), ct);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ((int)notFound.StatusCode).ShouldBeInRange(400, 499);
        unauthorized.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await Eventually.WithinAsync(() => Task.FromResult(_factory.ErrorReports.Reports.Any(r => r.CorrelationId == correlationId)), Limit, ct);
        var report = _factory.ErrorReports.Reports.Single(r => r.CorrelationId == correlationId);
        report.Source.ShouldBe(ErrorReportSource.Api);
        report.Status.ShouldBe(ErrorReportStatus.New);
        report.UserId.ShouldBe(userId);
        report.ProductVersion.ShouldNotBeNullOrWhiteSpace();
        report.Description!.ShouldContain("InvalidOperationException");
        report.Description!.ShouldNotContain("THROWINGKEY123");
        _factory.ErrorReports.Reports.Count(r => r.Source == ErrorReportSource.Api).ShouldBe(apiBefore + 1);
    }
}
