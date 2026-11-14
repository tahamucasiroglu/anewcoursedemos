using System.Buffers.Binary;
using System.Text;

namespace TahaKonsept.Agent.Core.Audio;

/// <summary>
/// Bağımlılıksız WAV (RIFF) yardımcıları: başlık okuma (<see cref="TryReadInfo"/>), aynı biçimli dosyaları birleştirme
/// (<see cref="Concatenate"/>) ve sessizlik yazma (<see cref="WriteSilence"/>).
/// </summary>
public static class WavFile
{
    /// <summary>Varsayılan sessizlik biçimi için örnekleme hızı.</summary>
    public const int DefaultSampleRate = 24000;

    /// <summary>WAV başlığını okur.</summary>
    /// <param name="path">Dosya.</param>
    /// <param name="info">Bilgi.</param>
    /// <returns>Geçerli WAV ise <c>true</c>.</returns>
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
            using var reader = new BinaryReader(stream);
            if (stream.Length < 12 || Ascii(reader.ReadBytes(4)) != "RIFF")
            {
                return false;
            }

            reader.ReadUInt32();
            if (Ascii(reader.ReadBytes(4)) != "WAVE")
            {
                return false;
            }

            byte[]? fmt = null;
            while (stream.Position + 8 <= stream.Length)
            {
                var id = Ascii(reader.ReadBytes(4));
                var size = reader.ReadUInt32();
                if (id == "fmt ")
                {
                    fmt = reader.ReadBytes((int)size);
                    if (size % 2 == 1 && stream.Position < stream.Length)
                    {
                        stream.Position++;
                    }

                    continue;
                }

                if (id == "data")
                {
                    if (fmt is null || fmt.Length < 16)
                    {
                        return false;
                    }

                    var offset = stream.Position;
                    var available = stream.Length - offset;
                    var length = size == uint.MaxValue || size > available ? available : size;
                    info = new WavInfo(
                        BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(0, 2)),
                        BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(2, 2)),
                        (int)BinaryPrimitives.ReadUInt32LittleEndian(fmt.AsSpan(4, 4)),
                        BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(14, 2)),
                        BinaryPrimitives.ReadUInt16LittleEndian(fmt.AsSpan(12, 2)),
                        fmt,
                        offset,
                        length);
                    return true;
                }

                stream.Position = Math.Min(stream.Length, stream.Position + size + (size % 2));
            }

            return false;
        }
        catch (IOException)
        {
            // EndOfStreamException da IOException'dır: kısa/bozuk dosya.
            return false;
        }
    }

    /// <summary>Aynı biçimli WAV dosyalarını tek dosyada birleştirir (ilk dosyanın <c>fmt </c> bölümü korunur).</summary>
    /// <param name="inputs">Sıralı girdi bilgileri (hepsi <see cref="WavInfo.IsCompatibleWith"/>).</param>
    /// <param name="inputPaths">Girdi dosyaları (aynı sırada).</param>
    /// <param name="outputPath">Çıktı.</param>
    /// <returns>Toplam süre (sn).</returns>
    public static double Concatenate(IReadOnlyList<WavInfo> inputs, IReadOnlyList<string> inputPaths, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(inputPaths);
        if (inputs.Count == 0 || inputs.Count != inputPaths.Count)
        {
            throw new ArgumentException("Girdi listesi boş ya da uyumsuz.", nameof(inputs));
        }

        var first = inputs[0];
        var totalData = inputs.Sum(i => i.DataLength);
        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            WriteHeader(output, first.FormatChunk, totalData);
            var buffer = new byte[81920];
            for (var i = 0; i < inputs.Count; i++)
            {
                using var input = File.OpenRead(inputPaths[i]);
                input.Position = inputs[i].DataOffset;
                var remaining = inputs[i].DataLength;
                while (remaining > 0)
                {
                    var read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                    if (read <= 0)
                    {
                        break;
                    }

                    output.Write(buffer, 0, read);
                    remaining -= read;
                }
            }

            if (totalData % 2 == 1)
            {
                output.WriteByte(0);
            }
        }

        return first.SampleRate > 0 && first.BlockAlign > 0 ? (double)totalData / ((long)first.SampleRate * first.BlockAlign) : 0d;
    }

    /// <summary>Sessiz WAV yazar (biçim verilmezse 24 kHz, mono, 16 bit PCM).</summary>
    /// <param name="outputPath">Çıktı.</param>
    /// <param name="seconds">Süre (sn).</param>
    /// <param name="format">Biçim örneği (isteğe bağlı).</param>
    public static void WriteSilence(string outputPath, double seconds, WavInfo? format = null)
    {
        var fmt = format?.FormatChunk ?? CreatePcmFormatChunk(DefaultSampleRate, 1, 16);
        var sampleRate = format?.SampleRate ?? DefaultSampleRate;
        var blockAlign = format?.BlockAlign ?? 2;
        var frames = (long)Math.Round(Math.Max(0, seconds) * sampleRate);
        var dataLength = frames * blockAlign;
        using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        WriteHeader(output, fmt, dataLength);
        var zeros = new byte[8192];
        var remaining = dataLength;
        while (remaining > 0)
        {
            var count = (int)Math.Min(zeros.Length, remaining);
            output.Write(zeros, 0, count);
            remaining -= count;
        }

        if (dataLength % 2 == 1)
        {
            output.WriteByte(0);
        }
    }

    /// <summary>Standart PCM <c>fmt </c> bölümü üretir.</summary>
    /// <param name="sampleRate">Örnekleme hızı.</param>
    /// <param name="channels">Kanal.</param>
    /// <param name="bitsPerSample">Örnek başına bit.</param>
    /// <returns>16 baytlık bölüm.</returns>
    public static byte[] CreatePcmFormatChunk(int sampleRate, int channels, int bitsPerSample)
    {
        var blockAlign = channels * bitsPerSample / 8;
        var chunk = new byte[16];
        BinaryPrimitives.WriteUInt16LittleEndian(chunk.AsSpan(0, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(chunk.AsSpan(2, 2), (ushort)channels);
        BinaryPrimitives.WriteUInt32LittleEndian(chunk.AsSpan(4, 4), (uint)sampleRate);
        BinaryPrimitives.WriteUInt32LittleEndian(chunk.AsSpan(8, 4), (uint)(sampleRate * blockAlign));
        BinaryPrimitives.WriteUInt16LittleEndian(chunk.AsSpan(12, 2), (ushort)blockAlign);
        BinaryPrimitives.WriteUInt16LittleEndian(chunk.AsSpan(14, 2), (ushort)bitsPerSample);
        return chunk;
    }

    private static void WriteHeader(Stream output, byte[] formatChunk, long dataLength)
    {
        var pad = formatChunk.Length % 2;
        var riffSize = 4 + 8 + formatChunk.Length + pad + 8 + dataLength + (dataLength % 2);
        using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write((uint)Math.Min(uint.MaxValue, riffSize));
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write((uint)formatChunk.Length);
        writer.Write(formatChunk);
        if (pad == 1)
        {
            writer.Write((byte)0);
        }

        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write((uint)Math.Min(uint.MaxValue, dataLength));
    }

    private static string Ascii(byte[] bytes) => Encoding.ASCII.GetString(bytes);
}
