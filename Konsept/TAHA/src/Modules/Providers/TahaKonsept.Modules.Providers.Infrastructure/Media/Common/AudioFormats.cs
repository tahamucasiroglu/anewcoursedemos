using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

/// <summary>
/// Ses biçimi yardımcıları: istenen biçimi desteklenen ikiliye indirger (<see cref="NormalizeFormat"/>: <c>mp3</c> / <c>wav</c>),
/// MIME türü (<see cref="MimeOf"/>), ham PCM'i WAV'a sarma (<see cref="WrapPcm16AsWav"/>; Gemini TTS, ElevenLabs PCM),
/// MIME'deki örnekleme hızını okuma (<see cref="SampleRateFromMime"/>) ve ses süresini ölçme (<see cref="DurationSeconds"/>:
/// WAV başlığından kesin, MP3'te kare kare tarayarak).
/// </summary>
public static class AudioFormats
{
    /// <summary>MP3 biçim kimliği.</summary>
    public const string Mp3 = "mp3";

    /// <summary>WAV biçim kimliği.</summary>
    public const string Wav = "wav";

    /// <summary>MPEG-1 Layer III bit hızları (kbps).</summary>
    private static readonly int[] Mpeg1Bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0];

    /// <summary>MPEG-2/2.5 Layer III bit hızları (kbps).</summary>
    private static readonly int[] Mpeg2Bitrates = [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0];

    /// <summary>MPEG-1 örnekleme hızları.</summary>
    private static readonly int[] Mpeg1Rates = [44100, 48000, 32000, 0];

    /// <summary>İstenen biçimi <c>wav</c> ya da <c>mp3</c>'e indirger (bilinmeyen / boş → mp3).</summary>
    /// <param name="format">İstenen biçim.</param>
    public static string NormalizeFormat(string? format) =>
        string.Equals(format?.Trim(), Wav, StringComparison.OrdinalIgnoreCase) ? Wav : Mp3;

    /// <summary>Biçimin MIME türü.</summary>
    /// <param name="format">Biçim (<c>mp3</c> / <c>wav</c>).</param>
    public static string MimeOf(string format) => NormalizeFormat(format) == Wav ? "audio/wav" : "audio/mpeg";

    /// <summary>16 bit küçük-endian ham PCM'i WAV (RIFF) dosyasına sarar.</summary>
    /// <param name="pcm">Ham PCM.</param>
    /// <param name="sampleRate">Örnekleme hızı.</param>
    /// <param name="channels">Kanal sayısı.</param>
    public static byte[] WrapPcm16AsWav(byte[] pcm, int sampleRate, int channels = 1)
    {
        ArgumentNullException.ThrowIfNull(pcm);
        const short bitsPerSample = 16;
        var blockAlign = (short)(channels * bitsPerSample / 8);
        var byteRate = sampleRate * blockAlign;
        var wav = new byte[44 + pcm.Length];
        var span = wav.AsSpan();
        Encoding.ASCII.GetBytes("RIFF").CopyTo(span);
        BinaryPrimitives.WriteInt32LittleEndian(span[4..], 36 + pcm.Length);
        Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(span[8..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1);
        BinaryPrimitives.WriteInt16LittleEndian(span[22..], (short)channels);
        BinaryPrimitives.WriteInt32LittleEndian(span[24..], sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(span[28..], byteRate);
        BinaryPrimitives.WriteInt16LittleEndian(span[32..], blockAlign);
        BinaryPrimitives.WriteInt16LittleEndian(span[34..], bitsPerSample);
        Encoding.ASCII.GetBytes("data").CopyTo(span[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(span[40..], pcm.Length);
        pcm.CopyTo(span[44..]);
        return wav;
    }

    /// <summary>MIME'deki <c>rate=</c> parametresini okur (ör. <c>audio/L16;codec=pcm;rate=24000</c>).</summary>
    /// <param name="mimeType">MIME türü.</param>
    /// <param name="fallback">Bulunamazsa.</param>
    public static int SampleRateFromMime(string? mimeType, int fallback)
    {
        foreach (var part in (mimeType ?? string.Empty).Split(';', StringSplitOptions.TrimEntries))
        {
            if (part.StartsWith("rate=", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(part[5..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rate) && rate > 0)
            {
                return rate;
            }
        }

        return fallback;
    }

    /// <summary>Ses süresi (saniye, 3 ondalık). WAV ve MP3 desteklenir; ölçülemezse null.</summary>
    /// <param name="audio">Ses dosyası.</param>
    public static decimal? DurationSeconds(byte[] audio)
    {
        ArgumentNullException.ThrowIfNull(audio);
        var seconds = IsWav(audio) ? WavSeconds(audio) : Mp3Seconds(audio);
        return seconds is > 0 ? decimal.Round((decimal)seconds.Value, 3) : null;
    }

    /// <summary>RIFF/WAVE başlığı var mı.</summary>
    /// <param name="audio">Ses.</param>
    private static bool IsWav(byte[] audio) =>
        audio.Length >= 12 && audio.AsSpan(0, 4).SequenceEqual("RIFF"u8) && audio.AsSpan(8, 4).SequenceEqual("WAVE"u8);

    /// <summary>WAV süresi: <c>data</c> boyutu / <c>fmt</c> bayt hızı.</summary>
    /// <param name="audio">WAV dosyası.</param>
    private static double? WavSeconds(byte[] audio)
    {
        var byteRate = 0;
        var offset = 12;
        while (offset + 8 <= audio.Length)
        {
            var id = audio.AsSpan(offset, 4);
            var size = BinaryPrimitives.ReadInt32LittleEndian(audio.AsSpan(offset + 4, 4));
            if (id.SequenceEqual("fmt "u8) && offset + 20 <= audio.Length)
            {
                byteRate = BinaryPrimitives.ReadInt32LittleEndian(audio.AsSpan(offset + 16, 4));
            }
            else if (id.SequenceEqual("data"u8))
            {
                // Akış hâlinde üretilen WAV'larda boyut 0 / 0xFFFFFFFF olabilir: dosyanın kalanı veri sayılır.
                var dataSize = size <= 0 || offset + 8 + size > audio.Length ? audio.Length - offset - 8 : size;
                return byteRate > 0 ? (double)dataSize / byteRate : null;
            }

            if (size < 0)
            {
                return null;
            }

            offset += 8 + size + (size % 2);
        }

        return null;
    }

    /// <summary>MP3 süresi: ID3v2 etiketini atlar, Layer III karelerini tek tek sayar (VBR dahil).</summary>
    /// <param name="audio">MP3 dosyası.</param>
    private static double? Mp3Seconds(byte[] audio)
    {
        var offset = 0;
        if (audio.Length >= 10 && audio.AsSpan(0, 3).SequenceEqual("ID3"u8))
        {
            var tagSize = (audio[6] & 0x7F) << 21 | (audio[7] & 0x7F) << 14 | (audio[8] & 0x7F) << 7 | (audio[9] & 0x7F);
            offset = 10 + tagSize + ((audio[5] & 0x10) != 0 ? 10 : 0);
        }

        double seconds = 0;
        var frames = 0;
        while (offset + 4 <= audio.Length)
        {
            var frame = ReadMp3Frame(audio, offset);
            if (frame is null)
            {
                offset++;
                continue;
            }

            seconds += frame.Value.Seconds;
            frames++;
            offset += frame.Value.Length;
        }

        return frames > 0 ? seconds : null;
    }

    /// <summary>Konumdaki MPEG Layer III kare başlığını çözer.</summary>
    /// <param name="audio">Dosya.</param>
    /// <param name="offset">Konum.</param>
    /// <returns>Kare uzunluğu ve süresi ya da geçerli kare yoksa null.</returns>
    private static (int Length, double Seconds)? ReadMp3Frame(byte[] audio, int offset)
    {
        if (audio[offset] != 0xFF || (audio[offset + 1] & 0xE0) != 0xE0)
        {
            return null;
        }

        var version = (audio[offset + 1] >> 3) & 0x03;
        var layer = (audio[offset + 1] >> 1) & 0x03;
        var bitrateIndex = (audio[offset + 2] >> 4) & 0x0F;
        var rateIndex = (audio[offset + 2] >> 2) & 0x03;
        var padding = (audio[offset + 2] >> 1) & 0x01;
        if (version == 1 || layer != 1 || bitrateIndex is 0 or 15 || rateIndex == 3)
        {
            return null;
        }

        var mpeg1 = version == 3;
        var bitrate = (mpeg1 ? Mpeg1Bitrates : Mpeg2Bitrates)[bitrateIndex] * 1000;
        var sampleRate = Mpeg1Rates[rateIndex] / (mpeg1 ? 1 : version == 2 ? 2 : 4);
        var samples = mpeg1 ? 1152 : 576;
        var length = (samples / 8 * bitrate / sampleRate) + padding;
        return length < 4 ? null : (length, (double)samples / sampleRate);
    }
}
