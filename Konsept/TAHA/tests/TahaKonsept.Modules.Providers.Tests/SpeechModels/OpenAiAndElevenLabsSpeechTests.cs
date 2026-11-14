using Shouldly;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Media;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.SpeechModels;

/// <summary>
/// OpenAI gpt-4o-mini-tts (audio/speech, voice + Türkçe talimat, dakika maliyeti) ve ElevenLabs (text-to-speech/{voice_id},
/// xi-api-key, model_id, bin karakter maliyeti, PCM → WAV) istek ve yanıt biçimleri.
/// </summary>
public sealed class OpenAiAndElevenLabsSpeechTests
{
    private readonly MediaFakeHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>OpenAI: istek biçimi, MP3 süresi ölçülür, maliyet dakika başı.</summary>
    [Fact]
    public async Task OpenAi_speech_request_and_minute_cost()
    {
        var mp3 = MediaTestData.Mp3(frames: 2297); // 2297 × 1152 / 44100 ≈ 60.003 sn
        _http.Bytes(mp3, "audio/mpeg");
        var model = new OpenAiSpeechModel(
            TestFixtures.Model("gpt-4o-mini-tts"), TestFixtures.Credential(ProviderPlatforms.OpenAi), TestFixtures.Dependencies(_recorder), _http.CreateClient());

        var result = await model.SynthesizeAsync(MediaTestData.Speech(voice: "coral"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.MimeType.ShouldBe("audio/mpeg");
        result.Value.AudioSeconds.ShouldBe(60.003m);
        result.Value.CostUsd.ShouldBe(0.015001m);
        var request = _http.Requests[0];
        request.Uri.ToString().ShouldBe("https://api.openai.com/v1/audio/speech");
        request.Headers["Authorization"].ShouldBe("Bearer sk-test-1234567890");
        request.Json["model"]!.GetValue<string>().ShouldBe("gpt-4o-mini-tts-2025-12-15");
        request.Json["voice"]!.GetValue<string>().ShouldBe("coral");
        request.Json["response_format"]!.GetValue<string>().ShouldBe("mp3");
        request.Json["instructions"]!.GetValue<string>().ShouldContain("Türkçe");
    }

    /// <summary>Ses verilmezse model istek atmadan doğrulama hatası döner.</summary>
    [Fact]
    public async Task Voice_is_required()
    {
        var model = new OpenAiSpeechModel(
            TestFixtures.Model("gpt-4o-mini-tts"), TestFixtures.Credential(ProviderPlatforms.OpenAi), TestFixtures.Dependencies(_recorder), _http.CreateClient());

        var result = await model.SynthesizeAsync(MediaTestData.Speech(voice: null), TestContext.Current.CancellationToken);

        result.Error.Code.ShouldBe(ProviderErrorCodes.InvalidRequest);
        _http.Requests.ShouldBeEmpty();
    }

    /// <summary>ElevenLabs: MP3 isteği, başlık, gövde ve bin karakter maliyeti.</summary>
    [Fact]
    public async Task ElevenLabs_mp3()
    {
        _http.Bytes(MediaTestData.Mp3(frames: 100), "audio/mpeg");
        var request = MediaTestData.Speech(voice: "pNInz6obpgDQGcFmaJgB");

        var result = await ElevenLabs("elevenlabs-v4").SynthesizeAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.AudioSeconds.ShouldBe(2.612m);
        result.Value.CostUsd.ShouldBe(decimal.Round(request.Text.Length * 0.08m / 1000m, 6));
        var sent = _http.Requests[0];
        sent.Uri.ToString().ShouldBe("https://api.elevenlabs.io/v1/text-to-speech/pNInz6obpgDQGcFmaJgB?output_format=mp3_44100_128");
        sent.Headers["xi-api-key"].ShouldBe("sk-test-1234567890");
        sent.Json["model_id"]!.GetValue<string>().ShouldBe("eleven_v4");
        sent.Json["language_code"]!.GetValue<string>().ShouldBe("tr");
        sent.Json["text"]!.GetValue<string>().ShouldBe(request.Text);
    }

    /// <summary>ElevenLabs: WAV isteğinde PCM alınır ve WAV'a sarılır; hata gövdesi (detail.message) taşınır.</summary>
    [Fact]
    public async Task ElevenLabs_wav_and_error_detail()
    {
        _http.Bytes(MediaTestData.Pcm(seconds: 2, rate: 44100), "audio/pcm")
            .Json("""{"detail":{"status":"voice_not_found","message":"Voice not found"}}""", System.Net.HttpStatusCode.BadRequest);
        var model = ElevenLabs("elevenlabs-v4-turbo");
        var ct = TestContext.Current.CancellationToken;

        var wav = await model.SynthesizeAsync(MediaTestData.Speech(voice: "x", format: "wav"), ct);
        wav.IsSuccess.ShouldBeTrue(wav.Error.Message);
        wav.Value.MimeType.ShouldBe("audio/wav");
        wav.Value.AudioSeconds.ShouldBe(2m);
        wav.Value.Audio.Take(4).ShouldBe("RIFF"u8.ToArray());
        _http.Requests[0].Uri.Query.ShouldContain("output_format=pcm_44100");

        var failed = await model.SynthesizeAsync(MediaTestData.Speech(voice: "yok"), ct);
        failed.Error.Code.ShouldBe(ProviderErrorCodes.BadRequest);
        failed.Error.Message.ShouldContain("Voice not found");
    }

    /// <summary>ElevenLabs modeli.</summary>
    /// <param name="key">Katalog anahtarı.</param>
    private ElevenLabsSpeechModel ElevenLabs(string key) =>
        new(TestFixtures.Model(key), TestFixtures.Credential(ProviderPlatforms.ElevenLabs), TestFixtures.Dependencies(_recorder), _http.CreateClient());
}
