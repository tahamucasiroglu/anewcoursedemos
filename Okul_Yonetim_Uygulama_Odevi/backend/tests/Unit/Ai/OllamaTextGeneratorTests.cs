using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Ai;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0012 AC-9 ve AC-6 (adaptör tarafı): Ollama adaptörü ayardaki modeli akışsız <c>api/chat</c> isteğiyle çağırır
/// (talimat sistem, içerik kullanıcı mesajı; düşük sıcaklık, token sınırı, <c>keep_alive</c>) ve yanıt metnini döndürür;
/// hata yanıtı, zaman aşımı, ulaşılamayan servis, JSON olmayan ve boş yanıt "kullanılamıyor" olur; iş sırası doluysa servis
/// hiç çağrılmadan "kullanılamıyor". Servis gerekmez (sahte HTTP).
/// </summary>
public sealed class OllamaTextGeneratorTests
{
    private static readonly AiTextRequest Request = new("Sabit talimat", "ali bugün ödevini getirmedi", 400);

    [Fact]
    public async Task GenerateAsync_Answer_PostsNonStreamingChatWithModel_ReturnsText()
    {
        FakeHttpHandler handler = FakeHttpHandler.Json("""{"model":"gemma3:4b","message":{"role":"assistant","content":"  Sayın veli, Ali bugün ödevini getirmedi.  "},"done":true}""");

        AiTextResult result = await Generator(handler).GenerateAsync(Request, TestContext.Current.CancellationToken);

        result.Text.ShouldBe("Sayın veli, Ali bugün ödevini getirmedi.");
        handler.Path.ShouldBe("/api/chat");
        handler.ContentType!.ShouldStartWith("application/json");
        using JsonDocument body = JsonDocument.Parse(handler.BodyText);
        JsonElement root = body.RootElement;
        root.GetProperty("model").GetString().ShouldBe("gemma3:4b");
        root.GetProperty("stream").GetBoolean().ShouldBeFalse();
        root.GetProperty("keep_alive").GetString().ShouldBe(OllamaTextGenerator.KeepAlive);
        JsonElement[] messages = [.. root.GetProperty("messages").EnumerateArray()];
        messages.Length.ShouldBe(2);
        messages[0].GetProperty("role").GetString().ShouldBe("system");
        messages[0].GetProperty("content").GetString().ShouldBe("Sabit talimat");
        messages[1].GetProperty("role").GetString().ShouldBe("user");
        messages[1].GetProperty("content").GetString().ShouldBe("ali bugün ödevini getirmedi");
        root.GetProperty("options").GetProperty("temperature").GetDouble().ShouldBe(OllamaTextGenerator.Temperature);
        root.GetProperty("options").GetProperty("num_predict").GetInt32().ShouldBe(400);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, "http-500")]
    [InlineData(HttpStatusCode.NotFound, "http-404")]
    public async Task GenerateAsync_ErrorStatus_Unavailable(HttpStatusCode status, string reason)
    {
        FakeHttpHandler handler = FakeHttpHandler.Json("""{"error":"model not found"}""", status);

        AiUnavailableException error = await Should.ThrowAsync<AiUnavailableException>(() => Generator(handler).GenerateAsync(Request, TestContext.Current.CancellationToken));

        error.Reason.ShouldBe(reason);
    }

    [Fact]
    public async Task GenerateAsync_Timeout_Unavailable()
    {
        FakeHttpHandler handler = FakeHttpHandler.Hanging();

        AiUnavailableException error = await Should.ThrowAsync<AiUnavailableException>(() => Generator(handler, TimeSpan.FromMilliseconds(200)).GenerateAsync(Request, TestContext.Current.CancellationToken));

        error.Reason.ShouldBe("timeout");
    }

    [Fact]
    public async Task GenerateAsync_Unreachable_Unavailable()
    {
        AiUnavailableException error = await Should.ThrowAsync<AiUnavailableException>(() => Generator(FakeHttpHandler.Refused()).GenerateAsync(Request, TestContext.Current.CancellationToken));

        error.Reason.ShouldBe("unreachable");
    }

    [Theory]
    [InlineData("""{"message":{"role":"assistant","content":"   "},"done":true}""", "empty")]
    [InlineData("""{"message":{"role":"assistant","content":""},"done":true}""", "empty")]
    [InlineData("""{"done":true}""", "empty")]
    [InlineData("<html>hata</html>", "invalid-response")]
    public async Task GenerateAsync_EmptyOrInvalidAnswer_Unavailable(string answer, string reason)
    {
        AiUnavailableException error = await Should.ThrowAsync<AiUnavailableException>(() => Generator(FakeHttpHandler.Json(answer)).GenerateAsync(Request, TestContext.Current.CancellationToken));

        error.Reason.ShouldBe(reason);
    }

    [Fact]
    public async Task GenerateAsync_GateBusy_UnavailableWithoutCallingService()
    {
        using AiWorkGate gate = new(TimeSpan.FromMilliseconds(100));
        TaskCompletionSource<int> release = new();
        Task<int> running = gate.RunAsync(_ => release.Task, TestContext.Current.CancellationToken);
        FakeHttpHandler handler = FakeHttpHandler.Json("""{"message":{"content":"metin"}}""");

        AiUnavailableException error = await Should.ThrowAsync<AiUnavailableException>(() => Generator(handler, gate: gate).GenerateAsync(Request, TestContext.Current.CancellationToken));

        error.Reason.ShouldBe("busy");
        handler.Calls.ShouldBe(0);
        release.SetResult(1);
        await running;
        (await Generator(handler, gate: gate).GenerateAsync(Request, TestContext.Current.CancellationToken)).Text.ShouldBe("metin", "sıra boşalınca çalışır");
    }

    private static OllamaTextGenerator Generator(FakeHttpHandler handler, TimeSpan? timeout = null, AiWorkGate? gate = null)
    {
        AiOptions options = new();
        options.TextGenerator.Model = "gemma3:4b";
        return new OllamaTextGenerator(handler.Client("http://127.0.0.1:11434/", timeout), Options.Create(options), gate ?? new AiWorkGate(TimeSpan.FromSeconds(5)));
    }
}
