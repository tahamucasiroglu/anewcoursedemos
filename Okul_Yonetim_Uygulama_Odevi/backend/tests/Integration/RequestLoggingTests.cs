using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using OkulYonetim.Tests.Integration.Infrastructure;
using Shouldly;

namespace OkulYonetim.Tests.Integration;

/// <summary>Gerçek API hattı üzerinden loglama testleri (spec 0001 AC-13).</summary>
[Trait("Category", "Integration")]
public sealed class RequestLoggingTests
{
    private readonly TestDatabase database;

    public RequestLoggingTests(TestDatabase database)
    {
        this.database = database;
    }

    [Fact]
    public async Task Request_WritesDailyFileLine_WithTraceId_NoBody()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/system-health", UriKind.Relative), TestContext.Current.CancellationToken);
        using StringContent secretBody = new("{\"tc\":\"gizli-govde-12345\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage post = await client.PostAsync(new Uri("/api/system-health", UriKind.Relative), secretBody, TestContext.Current.CancellationToken);

        string traceId = response.Headers.GetValues("X-Trace-Id").Single();
        string today = Path.Combine(factory.LogFolder, "okul-api-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
        File.Exists(today).ShouldBeTrue("log günlük dosyaya yazılmalı");

        string log = factory.ReadLog();
        string[] requestLines = log.Split('\n').Where(l => l.Contains("HTTP GET /api/system-health responded", StringComparison.Ordinal)).ToArray();
        requestLines.Length.ShouldBe(1, "tek istek tek satır olmalı; log:\n" + log);
        string requestLine = requestLines[0];
        requestLine.ShouldContain(traceId);
        requestLine.ShouldContain("responded 200");
        requestLine.ShouldContain(" ms");
        log.ShouldNotContain("gizli-govde-12345");
        log.ShouldNotContain("Sistem çalışıyor");
    }

    [Fact]
    public async Task UnhandledError_LoggedOnce_WithResponseTraceId()
    {
        using OkulApiFactory factory = new(this.database.ConnectionString, addThrowingEndpoint: true);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri(ThrowingEndpointStartupFilter.Path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        string bodyText = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        bodyText.ShouldNotContain(ThrowingEndpointStartupFilter.SecretMessage);
        JsonElement body = JsonDocument.Parse(bodyText).RootElement;
        string traceId = body.GetProperty("traceId").GetString()!;
        response.Headers.GetValues("X-Trace-Id").Single().ShouldBe(traceId);
        body.GetProperty("title").GetString()!.ShouldStartWith("Beklenmeyen bir hata oluştu");

        string[] errorLines = factory.ReadLog().Split('\n')
            .Where(l => l.Contains("[ERR]", StringComparison.Ordinal) && l.Contains(traceId, StringComparison.Ordinal))
            .ToArray();
        errorLines.Length.ShouldBe(1);
        factory.ReadLog().ShouldContain(ThrowingEndpointStartupFilter.SecretMessage);
    }
}
