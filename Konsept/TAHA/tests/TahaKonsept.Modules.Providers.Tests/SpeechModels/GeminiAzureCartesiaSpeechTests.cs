using Shouldly;
using TahaKonsept.Modules.Providers.Application.Common;
using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.SpeechModels.Concrete;
using TahaKonsept.Modules.Providers.Tests.Media;
using TahaKonsept.Modules.Providers.Tests.Support;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.SpeechModels;

/// <summary>
/// Gemini TTS (AUDIO modalitesi, prebuilt ses, PCM → WAV, token maliyeti), Azure Neural (SSML, bölge, çıktı biçimi başlığı,
/// bin karakter maliyeti) ve Cartesia (/tts/bytes, sürüm başlığı, voice id) istek ve yanıt biçimleri.
/// </summary>
public sealed class GeminiAzureCartesiaSpeechTests
{
    private readonly MediaFakeHandler _http = new();
    private readonly RecordingCallRecorder _recorder = new();

    /// <summary>Gemini: PCM base64 → 24 kHz WAV, süre kesin, maliyet usageMetadata token'larından.</summary>
    [Fact]
    public async Task Gemini_tts_wraps_pcm_as_wav()
    {
        var pcm = Convert.ToBase64String(MediaTestData.Pcm(seconds: 3, rate: 24000));
        _http.Json($$$"""
            {"candidates":[{"content":{"parts":[{"inlineData":{"mimeType":"audio/L16;codec=pcm;rate=24000","data":"{{{pcm}}}"}}]},"finishReason":"STOP"}],
             "usageMetadata":{"promptTokenCount":20,"candidatesTokenCount":75}}
            """);
        var model = new GeminiSpeechModel(
            TestFixtures.Model("gemini-3.8-flash-tts"), TestFixtures.Credential(ProviderPlatforms.Google), TestFixtures.Dependencies(_recorder), _http.CreateClient());

        var result = await model.SynthesizeAsync(MediaTestData.Speech(voice: "Kore"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.MimeType.ShouldBe("audio/wav");
        result.Value.AudioSeconds.ShouldBe(3m);
        result.Value.Audio.Length.ShouldBe(44 + (3 * 24000 * 2));
        result.Value.CostUsd.ShouldBe(0.000685m); // 20 × $0.5/M + 75 × $9/M
        var request = _http.Requests[0];
        request.Uri.ToString().ShouldBe("https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash-tts-preview:generateContent");
        var config = request.Json["generationConfig"]!;
        config["responseModalities"]![0]!.GetValue<string>().ShouldBe("AUDIO");
        config["speechConfig"]!["voiceConfig"]!["prebuiltVoiceConfig"]!["voiceName"]!.GetValue<string>().ShouldBe("Kore");
    }

    /// <summary>Azure: bölgeli adres, SSML gövde, çıktı biçimi ve bin karakter maliyeti.</summary>
    [Fact]
    public async Task Azure_ssml_request()
    {
        _http.Bytes(MediaTestData.Mp3(frames: 50), "audio/mpeg");
        var request = MediaTestData.Speech(voice: "tr-TR-AhmetNeural") with { Text = "Ali & Veli <test>" };

        var result = await Azure("westeurope").SynthesizeAsync(request, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.CostUsd.ShouldBe(decimal.Round(request.Text.Length * 0.016m / 1000m, 6));
        var sent = _http.Requests[0];
        sent.Uri.ToString().ShouldBe("https://westeurope.tts.speech.microsoft.com/cognitiveservices/v1");
        sent.Headers["Ocp-Apim-Subscription-Key"].ShouldBe("sk-test-1234567890");
        sent.Headers["X-Microsoft-OutputFormat"].ShouldBe(AzureSpeechModel.Mp3OutputFormat);
        _http.ContentTypes[0].ShouldBe("application/ssml+xml");
        sent.Body.ShouldContain("<voice name=\"tr-TR-AhmetNeural\">");
        sent.Body.ShouldContain("Ali &amp; Veli &lt;test&gt;");
        sent.Body.ShouldContain("xml:lang=\"tr-TR\"");
        sent.Body.ShouldNotContain("<lang ");
    }

    /// <summary>Azure: çok dilli seste metin lang etiketiyle sarılır; hız prosody'ye yansır; bölge yoksa hata.</summary>
    [Fact]
    public async Task Azure_multilingual_speed_and_missing_region()
    {
        var ssml = AzureSpeechModel.BuildSsml("Merhaba", "en-US-AvaMultilingualNeural", "tr-TR", 1.2m);
        ssml.ShouldContain("<lang xml:lang=\"tr-TR\">Merhaba</lang>");
        ssml.ShouldContain("rate=\"+20%\"");
        AzureSpeechModel.BuildSsml("a", "tr-TR-EmelNeural", "tr-TR", 0.9m).ShouldContain("rate=\"-10%\"");

        var result = await Azure(null).SynthesizeAsync(MediaTestData.Speech(voice: "tr-TR-EmelNeural"), TestContext.Current.CancellationToken);
        result.Error.Code.ShouldBe(ProviderErrorCodes.KeyMissing);
        _http.Requests.ShouldBeEmpty();
    }

    /// <summary>Cartesia: /tts/bytes istek biçimi, WAV çıktı.</summary>
    [Fact]
    public async Task Cartesia_bytes()
    {
        var wav = Infrastructure.Media.Common.AudioFormats.WrapPcm16AsWav(MediaTestData.Pcm(seconds: 1, rate: 44100), 44100);
        _http.Bytes(wav, "audio/wav");
        var model = new CartesiaSpeechModel(
            TestFixtures.Model("cartesia-sonic-3.6"), TestFixtures.Credential(ProviderPlatforms.Cartesia), TestFixtures.Dependencies(_recorder), _http.CreateClient());

        var result = await model.SynthesizeAsync(MediaTestData.Speech(voice: "voice-uuid", format: "wav"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.Error.Message);
        result.Value.AudioSeconds.ShouldBe(1m);
        result.Value.MimeType.ShouldBe("audio/wav");
        var sent = _http.Requests[0];
        sent.Uri.ToString().ShouldBe("https://api.cartesia.ai/tts/bytes");
        sent.Headers["X-API-Key"].ShouldBe("sk-test-1234567890");
        sent.Headers["Cartesia-Version"].ShouldBe(CartesiaSpeechModel.ApiVersion);
        sent.Json["model_id"]!.GetValue<string>().ShouldBe("sonic-3.6");
        sent.Json["voice"]!["id"]!.GetValue<string>().ShouldBe("voice-uuid");
        sent.Json["language"]!.GetValue<string>().ShouldBe("tr");
        sent.Json["output_format"]!["container"]!.GetValue<string>().ShouldBe("wav");
        _recorder.Records[0].Usage.Characters.ShouldBe(MediaTestData.Speech().Text.Length);
    }

    /// <summary>Azure modeli.</summary>
    /// <param name="region">Bölge.</param>
    private AzureSpeechModel Azure(string? region) =>
        new(TestFixtures.Model("azure-neural"), new ProviderCredential(ProviderPlatforms.AzureSpeech, "sk-test-1234567890", region), TestFixtures.Dependencies(_recorder), _http.CreateClient());
}
