using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;
using Konsept.Shared.Kernel.Security;
using Konsept.Web.ApiClient;
using Konsept.Web.ApiClient.Concrete;
using Konsept.Web.ApiClient.Dtos;
using Konsept.Web.Tests.Support;
using NSubstitute;
using Shouldly;

namespace Konsept.Web.Tests;

/// <summary>
/// "Ajanı eşleştir" sayfası ve Ajax uçları, Web tarafı (spec 0012 AC-2, AC-4, AC-11, AC-14): sayfa giriş ister, kod alanının
/// görünür etiketi var ve hata alanla ilişkili ve duyurulur; durum alanında bağlantı; Ajax tarayıcı IP'sini iletir, Api'nin
/// Türkçe hatalarını (kod yok/kullanılmış/çok fazla deneme) aynen taşır ve CSRF ister.
/// </summary>
public sealed class AgentPairingPageTests
{
    /// <summary>
    /// Sayfa kimliksiz girişe yönlendirir; girişli kullanıcıya görünür etiketli kod alanı, tek form (Enter gönderir), Onayla/Reddet
    /// ve duyurulan hata alanı verir; alt durum alanında "Ajanı eşleştir" bağlantısı sayfayı açar.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task PairingPage_RequiresLogin_LabelledCodeField()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var anonymous = SignedInClient.CreateAnonymous(factory);
        using var redirect = await anonymous.GetAsync(new Uri("/ajan-eslestir", UriKind.Relative), ct);
        using var client = await factory.CreateSignedInClientAsync(ct);

        var page = await WebFactory.GetDocumentAsync(client, "/ajan-eslestir", ct);

        redirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        redirect.Headers.Location!.AbsolutePath.ShouldBe("/giris");
        page.QuerySelector("h1")!.TextContent.ShouldBe("Ajanı eşleştir");
        var input = page.QuerySelector("#pairing-code")!;
        page.QuerySelector("label[for='pairing-code']")!.TextContent.ShouldBe("Eşleştirme kodu");
        input.Closest("form")!.QuerySelector("button[type='submit']").ShouldNotBeNull();
        input.GetAttribute("aria-describedby").ShouldBe("pairing-error");
        page.QuerySelector("#pairing-error")!.GetAttribute("role").ShouldBe("alert");
        page.QuerySelector("#pairing-status")!.GetAttribute("aria-live").ShouldBe("polite");
        page.QuerySelector("#pairing-approve")!.TextContent.ShouldBe("Onayla");
        page.QuerySelector("#pairing-reject")!.TextContent.ShouldBe("Reddet");
        var link = page.QuerySelector("#status-agent-pair")!;
        link.GetAttribute("href").ShouldBe("/ajan-eslestir");
        link.TextContent.ShouldBe("Ajanı eşleştir");
    }

    /// <summary>
    /// Ajax onay/sorgu tarayıcı IP'siyle Api'ye gider ve Api'nin Türkçe hatası aynen döner (kod yok 404, kullanılmış 409, çok
    /// fazla deneme 429); CSRF belirteci olmadan istek reddedilir.
    /// </summary>
    /// <returns>Görev.</returns>
    [Fact]
    public async Task PairingAjax_ForwardsClientIp_MapsErrors()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new WebFactory();
        using var client = await factory.CreateSignedInClientAsync(ct);
        var page = await WebFactory.GetDocumentAsync(client, "/ajan-eslestir", ct);
        var token = page.QuerySelector("meta[name='csrf-token']")!.GetAttribute("content")!;
        factory.Api.AgentPairingAsync("lookup", "ABCD-2345", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PairingDeviceResponse>.Success(new PairingDeviceResponse("ABCD-2345", "TAHA-PC", "0.5.0", DateTimeOffset.UtcNow)));
        factory.Api.AgentPairingAsync("approve", "ZZZZ-9999", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PairingDeviceResponse>.Failure(new Error("agent_pairing.code_not_found", "Kod bulunamadı. Ajan penceresindeki kodu kontrol edin.", ErrorType.NotFound)));
        factory.Api.AgentPairingAsync("approve", "ABCD-2345", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PairingDeviceResponse>.Failure(new Error("agent_pairing.code_used", "Bu kod daha önce kullanılmış.", ErrorType.Conflict)));
        factory.Api.AgentPairingAsync("reject", "ABCD-2345", Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Result<PairingDeviceResponse>.Failure(new Error("auth.too_many_attempts", "Çok fazla deneme, biraz sonra tekrar deneyin", ErrorType.TooManyRequests)));

        var lookup = await PostAsync(client, "/ajan-eslestir/sorgula", "ABCD-2345", token, ct);
        var unknown = await PostAsync(client, "/ajan-eslestir/onayla", "ZZZZ-9999", token, ct);
        var used = await PostAsync(client, "/ajan-eslestir/onayla", "ABCD-2345", token, ct);
        var many = await PostAsync(client, "/ajan-eslestir/reddet", "ABCD-2345", token, ct);
        var noCsrf = await PostAsync(client, "/ajan-eslestir/onayla", "ABCD-2345", null, ct);

        lookup.Status.ShouldBe(HttpStatusCode.OK);
        lookup.Body.GetProperty("machineName").GetString().ShouldBe("TAHA-PC");
        unknown.Status.ShouldBe(HttpStatusCode.NotFound);
        unknown.Body.GetProperty("message").GetString().ShouldBe("Kod bulunamadı. Ajan penceresindeki kodu kontrol edin.");
        used.Status.ShouldBe(HttpStatusCode.Conflict);
        used.Body.GetProperty("message").GetString().ShouldBe("Bu kod daha önce kullanılmış.");
        many.Status.ShouldBe(HttpStatusCode.TooManyRequests);
        many.Body.GetProperty("message").GetString().ShouldBe("Çok fazla deneme, biraz sonra tekrar deneyin");
        noCsrf.Status.ShouldBe(HttpStatusCode.BadRequest);
        await factory.Api.Received(1).AgentPairingAsync("lookup", "ABCD-2345", Arg.Any<string?>(), Arg.Any<CancellationToken>()); // TestServer'da uzak IP yok; iletim aşağıda ApiClient düzeyinde.

        // ApiClient: istemci IP'si servis anahtarıyla gider; kod hiçbir adreste değil, yalnızca gövdede (review r1 M-2).
        var handler = new RecordingHandler("""{"code":"ABCD-2345","machineName":"TAHA-PC","agentVersion":"0.5.0","expiresAt":"2026-11-16T09:10:00Z"}""");
        var api = new KonseptApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") }, new ApiClientOptions("svc-key"));
        (await api.AgentPairingAsync("approve", " abcd-2345 ", "192.168.1.20", ct)).IsSuccess.ShouldBeTrue();
        handler.Requests[0].Method.ShouldBe(HttpMethod.Post);
        handler.Requests[0].RequestUri!.AbsolutePath.ShouldBe("/api/agent-pairing/approve");
        handler.Requests[0].RequestUri!.ToString().ShouldNotContain("abcd", Case.Insensitive);
        handler.Requests[0].Headers.GetValues(ClientIpHeader.Name).ShouldBe(["192.168.1.20"]);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(HttpClient client, string path, string code, string? token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(new { code }) };
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
