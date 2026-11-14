using System.Buffers.Binary;
using System.Text;

namespace TahaKonsept.Agent.Engines.Speech.Audio;

/// <summary>
/// WAV yardımcıları (durumsuz): mono float örnekleri 16-bit PCM WAV olarak yazar (<see cref="WriteMono16"/>),
/// başlığı okuyup süreyi hesaplar (<c>TryReadInfo</c>, <see cref="GetDurationSec"/>) ve örnek sayısından süre verir.
/// </summary>
public static class WavFile
{
    private const int HeaderBytes = 44;

    /// <summary>Örnek sayısı ve örnekleme hızından süreyi (sn) hesaplar.</summary>
    /// <param name="sampleCount">Örnek sayısı (mono).</param>
    /// <param name="sampleRate">Örnekleme hızı (Hz).</param>
    /// <returns>Süre (sn); hız geçersizse 0.</returns>
    public static double DurationSec(long sampleCount, int sampleRate) => sampleRate <= 0 ? 0 : (double)sampleCount / sampleRate;

    /// <summary>Mono float örnekleri (-1..1, taşanlar kırpılır) 16-bit PCM WAV olarak yazar; klasör yoksa oluşturur.</summary>
    /// <param name="path">Çıktı yolu.</param>
    /// <param name="samples">Örnekler.</param>
    /// <param name="sampleRate">Örnekleme hızı (Hz).</param>
    /// <returns>Yazılan sesin süresi (sn).</returns>
    public static double WriteMono16(string path, ReadOnlySpan<float> samples, int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dataBytes = samples.Length * 2;
        var buffer = new byte[HeaderBytes + dataBytes];
        WriteHeader(buffer, sampleRate, channels: 1, bitsPerSample: 16, dataBytes);
        var data = buffer.AsSpan(HeaderBytes);
        for (var i = 0; i < samples.Length; i++)
        {
            var value = samples[i];
            if (float.IsNaN(value))
            {
                value = 0f;
            }

            var clamped = Math.Clamp(value, -1f, 1f);
            BinaryPrimitives.WriteInt16LittleEndian(data.Slice(i * 2, 2), (short)MathF.Round(clamped * short.MaxValue));
        }

        File.WriteAllBytes(path, buffer);
        return DurationSec(samples.Length, sampleRate);
    }

    /// <summary>WAV başlığını okur (bölümleri sırayla tarar; "fmt " ve "data" sırası serbest).</summary>
    /// <param name="path">Dosya yolu.</param>
    /// <param name="info">Başarılıysa başlık bilgisi.</param>
    /// <returns>Geçerli bir RIFF/WAVE dosyasıysa <c>true</c>.</returns>
    public static bool TryReadInfo(string path, out WavInfo? info)
    {
        info = null;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var stream = File.OpenRead(path);
            return TryReadInfo(stream, out info);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Akıştan WAV başlığını okur.</summary>
    /// <param name="stream">Başında WAV olan akış.</param>
    /// <param name="info">Başarılıysa başlık bilgisi.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    public static bool TryReadInfo(Stream stream, out WavInfo? info)
    {
        info = null;
        Span<byte> header = stackalloc byte[12];
        if (!TryReadExactly(stream, header) ||
            !header[..4].SequenceEqual("RIFF"u8) ||
            !header.Slice(8, 4).SequenceEqual("WAVE"u8))
        {
            return false;
        }

        int sampleRate = 0, channels = 0, bits = 0;
        var haveFormat = false;
        Span<byte> chunkHeader = stackalloc byte[8];
        Span<byte> format = stackalloc byte[16];
        while (TryReadExactly(stream, chunkHeader))
        {
            var id = Encoding.ASCII.GetString(chunkHeader[..4]);
            long size = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader.Slice(4, 4));
            if (id == "fmt " && size >= 16)
            {
                if (!TryReadExactly(stream, format))
                {
                    return false;
                }

                channels = BinaryPrimitives.ReadInt16LittleEndian(format.Slice(2, 2));
                sampleRate = BinaryPrimitives.ReadInt32LittleEndian(format.Slice(4, 4));
                bits = BinaryPrimitives.ReadInt16LittleEndian(format.Slice(14, 2));
                haveFormat = true;
                Skip(stream, size - 16 + (size & 1));
            }
            else if (id == "data")
            {
                if (!haveFormat)
                {
                    return false;
                }

                // Akışla yazılmış dosyalarda boyut 0/0xFFFFFFFF olabilir: kalan bayttan hesapla.
                var remaining = stream.CanSeek ? stream.Length - stream.Position : size;
                var dataBytes = size == 0 || size == uint.MaxValue || size > remaining ? remaining : size;
                info = new WavInfo(sampleRate, channels, bits, dataBytes);
                return true;
            }
            else
            {
                Skip(stream, size + (size & 1));
            }
        }

        return false;
    }

    /// <summary>WAV dosyasının süresini (sn) okur.</summary>
    /// <param name="path">Dosya yolu.</param>
    /// <returns>Süre ya da okunamazsa <c>null</c>.</returns>
    public static double? GetDurationSec(string path) => TryReadInfo(path, out var info) && info is not null ? info.DurationSec : null;

    private static void WriteHeader(Span<byte> buffer, int sampleRate, short channels, short bitsPerSample, int dataBytes)
    {
        var blockAlign = (short)(channels * bitsPerSample / 8);
        "RIFF"u8.CopyTo(buffer);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(4, 4), 36 + dataBytes);
        "WAVE"u8.CopyTo(buffer[8..]);
        "fmt "u8.CopyTo(buffer[12..]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(16, 4), 16);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(20, 2), 1);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(22, 2), channels);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(24, 4), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(28, 4), sampleRate * blockAlign);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(32, 2), blockAlign);
        BinaryPrimitives.WriteInt16LittleEndian(buffer.Slice(34, 2), bitsPerSample);
        "data"u8.CopyTo(buffer[36..]);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(40, 4), dataBytes);
    }

    private static bool TryReadExactly(Stream stream, Span<byte> buffer)
    {
        try
        {
            stream.ReadExactly(buffer);
            return true;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }

    private static void Skip(Stream stream, long count)
    {
        if (count <= 0)
        {
            return;
        }

        if (stream.CanSeek)
        {
            stream.Seek(count, SeekOrigin.Current);
            return;
        }

        var scratch = new byte[Math.Min(count, 8192)];
        while (count > 0)
        {
            var read = stream.Read(scratch, 0, (int)Math.Min(count, scratch.Length));
            if (read == 0)
            {
                return;
            }

            count -= read;
        }
    }
}
