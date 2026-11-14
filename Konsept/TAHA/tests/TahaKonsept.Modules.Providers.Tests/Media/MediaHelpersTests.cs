using Shouldly;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;
using Xunit;

namespace TahaKonsept.Modules.Providers.Tests.Media;

/// <summary>
/// Ortak medya yardımcıları: WAV sarma ve süre, MP3 kare sayımı (ID3 atlama), MIME'den örnekleme hızı, boyut/oran seçimi,
/// hata gövdesinden mesaj çıkarma.
/// </summary>
public sealed class MediaHelpersTests
{
    /// <summary>PCM → WAV: başlık alanları ve süre.</summary>
    [Fact]
    public void Wav_wrap_and_duration()
    {
        var wav = AudioFormats.WrapPcm16AsWav(MediaTestData.Pcm(seconds: 2, rate: 24000), 24000);

        wav.Length.ShouldBe(44 + 96000);
        BitConverter.ToInt32(wav, 24).ShouldBe(24000);
        BitConverter.ToInt32(wav, 40).ShouldBe(96000);
        AudioFormats.DurationSeconds(wav).ShouldBe(2m);
    }

    /// <summary>MP3: ID3v2 etiketi atlanır, kareler sayılır.</summary>
    [Fact]
    public void Mp3_duration_skips_id3()
    {
        byte[] id3 = [(byte)'I', (byte)'D', (byte)'3', 4, 0, 0, 0, 0, 0, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        var mp3 = id3.Concat(MediaTestData.Mp3(frames: 441)).ToArray();

        AudioFormats.DurationSeconds(mp3).ShouldBe(11.52m);
        AudioFormats.DurationSeconds([1, 2, 3]).ShouldBeNull();
    }

    /// <summary>MIME'den hız, biçim indirgeme.</summary>
    [Fact]
    public void Mime_helpers()
    {
        AudioFormats.SampleRateFromMime("audio/L16;codec=pcm;rate=24000", 1).ShouldBe(24000);
        AudioFormats.SampleRateFromMime("audio/L16", 16000).ShouldBe(16000);
        AudioFormats.NormalizeFormat("WAV").ShouldBe("wav");
        AudioFormats.NormalizeFormat("ogg").ShouldBe("mp3");
        AudioFormats.MimeOf("wav").ShouldBe("audio/wav");
    }

    /// <summary>Oran, sabit boyut ve sığdırma.</summary>
    [Fact]
    public void Image_sizes()
    {
        ImageSizes.NearestAspectRatio(1080, 1920, ImageSizes.CommonAspectRatios).ShouldBe("9:16");
        ImageSizes.NearestAspectRatio(1024, 1024, ImageSizes.CommonAspectRatios).ShouldBe("1:1");
        ImageSizes.NearestSize(1920, 1080, ["1024x1024", "1024x1536", "1536x1024"]).ShouldBe("1536x1024");
        ImageSizes.Fit(1080, 1920, 16).ShouldBe((1088, 1920));
        var (w, h) = ImageSizes.Fit(2160, 3840, 16, 1L << 21);
        ((long)w * h).ShouldBeLessThanOrEqualTo(1L << 21);
        (w % 16).ShouldBe(0);
        (h % 16).ShouldBe(0);
    }

    /// <summary>Hata gövdesi biçimleri.</summary>
    [Fact]
    public void Error_messages()
    {
        MediaHttp.ExtractErrorMessage("""{"error":{"message":"Invalid size"}}""").ShouldBe("Invalid size");
        MediaHttp.ExtractErrorMessage("""{"detail":{"status":"x","message":"Voice not found"}}""").ShouldBe("Voice not found");
        MediaHttp.ExtractErrorMessage("""{"detail":[{"msg":"field required","loc":["body","prompt"]}]}""").ShouldContain("field required");
        MediaHttp.ExtractErrorMessage("""{"detail":"Not authenticated"}""").ShouldBe("Not authenticated");
        MediaHttp.ExtractErrorMessage("düz metin").ShouldBe("düz metin");
    }
}
