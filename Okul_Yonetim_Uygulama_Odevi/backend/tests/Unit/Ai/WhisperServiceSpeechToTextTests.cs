using System.Net;
using OkulYonetim.Application.Ports;
using OkulYonetim.Infrastructure.Ai;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Ai;

/// <summary>
/// Spec 0012 AC-9 ve AC-6 (adaptör tarafı): ses servisi adaptörü sesi ham gövde ve içerik türüyle <c>transcribe</c>'a
/// gönderir, dökümü ve süreyi okur; hata yanıtı, zaman aşımı, ulaşılamayan servis ve eksik alanlı yanıt "kullanılamıyor"
/// olur. Servis gerekmez (sahte HTTP).
/// </summary>
public sealed class WhisperServiceSpeechToTextTests
{
    private static readonly byte[] Audio = [1, 2, 3, 4, 5, 6, 7, 8];

    [Fact]
    public async Task TranscribeAsync_Answer_SendsRawAudioWithType_ReadsTextAndDuration()
    {
        FakeHttpHandler handler = FakeHttpHandler.Json("""{"text":"  ali bugün ödevini getirmedi  ","duration":21.5,"language":"tr"}""");
        using MemoryStream audio = new(Audio);

        SpeechToTextResult result = await Speech(handler).TranscribeAsync(audio, "audio/m4a", TestContext.Current.CancellationToken);

        result.Text.ShouldBe("ali bugün ödevini getirmedi");
        result.DurationSeconds.ShouldBe(21.5);
        handler.Path.ShouldBe("/transcribe");
        handler.ContentType.ShouldBe("audio/m4a");
        handler.Body.ShouldBe(Audio);
    }

    [Fact]
    public async Task TranscribeAsync_EmptyText_ReturnedForCallerToJudge()
    {
        using MemoryStream audio = new(Audio);

        SpeechToTextResult result = await Speech(FakeHttpHandler.Json("""{"text":"","duration":3.0}""")).TranscribeAsync(audio, "audio/wav", TestContext.Current.CancellationToken);

        result.Text.ShouldBeEmpty();
        result.DurationSeconds.ShouldBe(3.0);
    }

    [Fact]
    public async Task TranscribeAsync_TooLongSkippedByService_EmptyTextWithMeasuredDuration()
    {
        // Review round 1 (L-1): servis 60 sn üstü sesi dökmeden, ölçtüğü süreyle döner; karar (60 sn mesajı) çağıranda.
        using MemoryStream audio = new(Audio);
        FakeHttpHandler handler = FakeHttpHandler.Json("""{"text":"","duration":75.31,"language":"tr","elapsedMs":40,"skipped":"too-long"}""");

        SpeechToTextResult result = await Speech(handler).TranscribeAsync(audio, "audio/m4a", TestContext.Current.CancellationToken);

        result.Text.ShouldBeEmpty();
        result.DurationSeconds.ShouldBe(75.31);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"error":"model yükleniyor"}""", "http-503")]
    [InlineData(HttpStatusCode.BadRequest, """{"error":"ses çözülemedi"}""", "http-400")]
    [InlineData(HttpStatusCode.OK, """{"text":"metin"}""", "invalid-response")]
    [InlineData(HttpStatusCode.OK, """{"duration":4.0}""", "invalid-response")]
    [InlineData(HttpStatusCode.OK, "tamam", "invalid-response")]
    public async Task TranscribeAsync_ErrorOrIncompleteAnswer_Unavailable(HttpStatusCode status, string answer, string reason)
    {
        using MemoryStream audio = new(Audio);

        AiUnavailableException error = await Should.ThrowAsync<AiUnavailableException>(() => Speech(FakeHttpHandler.Json(answer, status)).TranscribeAsync(audio, "audio/webm", TestContext.Current.CancellationToken));

        error.Reason.ShouldBe(reason);
    }

    [Fact]
    public async Task TranscribeAsync_Timeout_Unavailable()
    {
        using MemoryStream audio = new(Audio);

        AiUnavailableException error = await Should.ThrowAsync<AiUnavailableException>(() => Speech(FakeHttpHandler.Hanging(), TimeSpan.FromMilliseconds(200)).TranscribeAsync(audio, "audio/ogg", TestContext.Current.CancellationToken));

        error.Reason.ShouldBe("timeout");
    }

    [Fact]
    public async Task TranscribeAsync_Unreachable_Unavailable()
    {
        using MemoryStream audio = new(Audio);

        AiUnavailableException error = await Should.ThrowAsync<AiUnavailableException>(() => Speech(FakeHttpHandler.Refused()).TranscribeAsync(audio, "audio/m4a", TestContext.Current.CancellationToken));

        error.Reason.ShouldBe("unreachable");
    }

    private static WhisperServiceSpeechToText Speech(FakeHttpHandler handler, TimeSpan? timeout = null) =>
        new(handler.Client("http://127.0.0.1:8765/", timeout), new AiWorkGate(TimeSpan.FromSeconds(5)));
}
