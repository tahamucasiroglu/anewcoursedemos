using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// Tarayıcının çağırdığı Web uçları: durum (API kapalı dahil) ve "Ajanı test et" (CSRF zorunlu) — AC-4, AC-7.
/// </summary>
public sealed class SystemEndpointTests : IClassFixture<WebFactory>
{
    private readonly WebFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="SystemEndpointTests"/> class.</summary>
    /// <param name="factory">Web fabrikası.</param>
    public SystemEndpointTests(WebFactory factory)
    {
        _factory = factory;
    }

    /// <summary>API'ye ulaşılamazsa durum 200 ile <c>apiAvailable=false</c> döner (sayfa "API erişilemiyor" gösterir).</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Status_ApiUnavailable_ReturnsApiDownState()
    {
        _factory.Api.GetSystemStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Result<SystemStatusResponse>.Failure(ApiClientErrors.Unavailable));
        using var client = _factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/system/status", TestContext.Current.CancellationToken);

        body.GetProperty("apiAvailable").GetBoolean().ShouldBeFalse();
    }

    /// <summary>API cevabı tarayıcıya sürüm, veritabanı, ajan ve son olayla iletilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task Status_ApiReachable_ReturnsStatus()
    {
        _factory.Api.GetSystemStatusAsync(Arg.Any<CancellationToken>()).Returns(Result<SystemStatusResponse>.Success(
            new SystemStatusResponse("0.1.0", false, new AgentStatusResponse(true, "TAHA-PC", "0.1.0", null), null)));
        using var client = _factory.CreateClient();

        var body = await client.GetFromJsonAsync<JsonElement>("/system/status", TestContext.Current.CancellationToken);

        body.GetProperty("apiAvailable").GetBoolean().ShouldBeTrue();
        body.GetProperty("version").GetString().ShouldBe("0.1.0");
        body.GetProperty("databaseAvailable").GetBoolean().ShouldBeFalse();
        body.GetProperty("agent").GetProperty("isConnected").GetBoolean().ShouldBeTrue();
    }

    /// <summary>CSRF belirteci olmadan POST reddedilir.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentTest_WithoutAntiforgeryToken_IsRejected()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsync(new Uri("/system/agent-test", UriKind.Relative), null, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>API "Zaman aşımı" derse tarayıcıya 504 ve aynı mesaj gider; başarıda ajan bilgisi döner.</summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task AgentTest_WithToken_ForwardsResultAndErrors()
    {
        using var client = _factory.CreateClient();
        var page = await WebFactory.GetDocumentAsync(client, "/", TestContext.Current.CancellationToken);
        var token = page.QuerySelector("meta[name='csrf-token']")!.GetAttribute("content")!;

        _factory.Api.TestAgentAsync(Arg.Any<CancellationToken>())
            .Returns(Result<AgentTestResponse>.Failure(new Error("agent.timeout", "Zaman aşımı", ErrorType.Timeout)));
        using var timeoutResponse = await PostWithTokenAsync(client, token);
        var timeoutBody = await timeoutResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        _factory.Api.TestAgentAsync(Arg.Any<CancellationToken>())
            .Returns(Result<AgentTestResponse>.Success(new AgentTestResponse("TAHA-PC", "0.1.0")));
        using var okResponse = await PostWithTokenAsync(client, token);
        var okBody = await okResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        timeoutResponse.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        timeoutBody.GetProperty("message").GetString().ShouldBe("Zaman aşımı");
        okResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        okBody.GetProperty("machineName").GetString().ShouldBe("TAHA-PC");
    }

    private static async Task<HttpResponseMessage> PostWithTokenAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/system/agent-test");
        request.Headers.Add("RequestVerificationToken", token);
        return await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(false);
    }
}
