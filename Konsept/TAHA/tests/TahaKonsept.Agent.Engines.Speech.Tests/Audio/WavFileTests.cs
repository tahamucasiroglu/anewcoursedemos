using System.Buffers.Binary;
using Shouldly;
using TahaKonsept.Agent.Engines.Speech.Audio;
using TahaKonsept.Agent.Engines.Speech.Tests.Support;
using Xunit;

namespace TahaKonsept.Agent.Engines.Speech.Tests.Audio;

/// <summary><see cref="WavFile"/> yazma/okuma ve süre hesabı testleri.</summary>
public sealed class WavFileTests
{
    /// <summary>Yazılan 16-bit mono dosyanın başlığı ve süresi doğru okunur.</summary>
    [Fact]
    public void Write_then_read_round_trips_header_and_duration()
    {
        var path = Path.Combine(TestEnvironment.NewTempDir("wav"), "nested", "a.wav");
        var samples = Enumerable.Range(0, 44100 * 2).Select(i => MathF.Sin(i * 0.05f) * 0.5f).ToArray();

        var duration = WavFile.WriteMono16(path, samples, 44100);

        duration.ShouldBe(2.0, 1e-9);
        WavFile.TryReadInfo(path, out var info).ShouldBeTrue();
        info.ShouldNotBeNull();
        info.SampleRate.ShouldBe(44100);
        info.Channels.ShouldBe(1);
        info.BitsPerSample.ShouldBe(16);
        info.DataBytes.ShouldBe(samples.Length * 2);
        info.DurationSec.ShouldBe(2.0, 1e-9);
        new FileInfo(path).Length.ShouldBe(44 + (samples.Length * 2));
    }

    /// <summary>Aralık dışı ve NaN örnekler kırpılır.</summary>
    [Fact]
    public void Samples_are_clamped_and_nan_becomes_silence()
    {
        var path = Path.Combine(TestEnvironment.NewTempDir("wav"), "clamp.wav");
        WavFile.WriteMono16(path, [2f, -2f, float.NaN, 0.5f], 8000);

        var bytes = File.ReadAllBytes(path).AsSpan(44);
        BinaryPrimitives.ReadInt16LittleEndian(bytes[..2]).ShouldBe(short.MaxValue);
        BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(2, 2)).ShouldBe((short)-short.MaxValue);
        BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(4, 2)).ShouldBe((short)0);
        BinaryPrimitives.ReadInt16LittleEndian(bytes.Slice(6, 2)).ShouldBe((short)16384);
    }

    /// <summary>"fmt " ile "data" arasında başka bölüm (LIST) olsa da süre okunur.</summary>
    [Fact]
    public void Reader_skips_unknown_chunks()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            var data = new byte[24000 * 2 * 2]; // 1 sn, 24 kHz, stereo 16-bit
            writer.Write("RIFF"u8);
            writer.Write(4 + 24 + 14 + 8 + data.Length);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)2);
            writer.Write(24000);
            writer.Write(24000 * 4);
            writer.Write((short)4);
            writer.Write((short)16);
            writer.Write("LIST"u8);
            writer.Write(5);
            writer.Write("INFOx"u8);
            writer.Write((byte)0); // tek boyutlu bölüm dolgusu
            writer.Write("data"u8);
            writer.Write(data.Length);
            writer.Write(data);
        }

        stream.Position = 0;
        WavFile.TryReadInfo(stream, out var info).ShouldBeTrue();
        info!.Channels.ShouldBe(2);
        info.DurationSec.ShouldBe(1.0, 1e-9);
    }

    /// <summary>WAV olmayan ya da olmayan dosya için false/null döner.</summary>
    [Fact]
    public void Invalid_or_missing_file_returns_false()
    {
        var dir = TestEnvironment.NewTempDir("wav");
        var text = Path.Combine(dir, "x.wav");
        File.WriteAllText(text, "bu bir wav değil");

        WavFile.TryReadInfo(text, out _).ShouldBeFalse();
        WavFile.GetDurationSec(Path.Combine(dir, "yok.wav")).ShouldBeNull();
    }

    /// <summary>Örnek sayısından süre.</summary>
    [Fact]
    public void Duration_from_sample_count()
    {
        WavFile.DurationSec(22050, 44100).ShouldBe(0.5);
        WavFile.DurationSec(100, 0).ShouldBe(0);
    }
}
