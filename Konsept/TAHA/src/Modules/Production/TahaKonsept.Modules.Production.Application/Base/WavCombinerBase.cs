using System.Buffers.Binary;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Application.Models;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IWavCombiner"/> ortak davranışı: her parçanın RIFF/WAVE başlığını okur (<c>fmt </c> ve <c>data</c> parçaları), biçimlerin aynı
/// olduğunu doğrular, ses verilerini art arda ekler, boş parçalar için sıfır (sessizlik) yazar ve 44 baytlık standart başlıkla tek WAV üretir.
/// Somut sınıf yalnızca kabul edilen biçimleri seçer (<see cref="IsSupported"/>).
/// </summary>
public abstract class WavCombinerBase : IWavCombiner
{
    /// <summary>Birleştirir.</summary>
    /// <param name="parts">Parçalar (boş sahne için <c>null</c>).</param>
    /// <param name="silenceSec">Boş sahne sessizlik süresi.</param>
    /// <returns>Birleşik ses ya da hata.</returns>
    public Result<CombinedAudio> Combine(IReadOnlyList<byte[]?> parts, double silenceSec)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var parsed = new List<(WavFormat Format, ArraySegment<byte> Data)?>(parts.Count);
        WavFormat? format = null;
        foreach (var part in parts)
        {
            if (part is null || part.Length == 0)
            {
                parsed.Add(null);
                continue;
            }

            var info = Parse(part);
            if (info is null)
            {
                return ProductionErrors.AudioCombineFailed("ses WAV biçiminde değil");
            }

            if (!IsSupported(info.Value.Format))
            {
                return ProductionErrors.AudioCombineFailed("desteklenmeyen WAV biçimi");
            }

            if (format is not null && format.Value != info.Value.Format)
            {
                return ProductionErrors.AudioCombineFailed("sahne seslerinin biçimleri farklı");
            }

            format ??= info.Value.Format;
            parsed.Add(info);
        }

        var fmt = format ?? new WavFormat(1, 1, 24000, 16, 2);
        using var data = new MemoryStream();
        var durations = new List<double>(parts.Count);
        foreach (var item in parsed)
        {
            if (item is null)
            {
                var frames = (int)Math.Round(silenceSec * fmt.SampleRate);
                data.Write(new byte[frames * fmt.BlockAlign]);
                durations.Add(frames / (double)fmt.SampleRate);
                continue;
            }

            data.Write(item.Value.Data);
            durations.Add(item.Value.Data.Count / (double)(fmt.BlockAlign * fmt.SampleRate));
        }

        var audio = BuildWav(fmt, data.ToArray());
        return new CombinedAudio(audio, durations, durations.Sum());
    }

    /// <summary>Biçim kabul ediliyor mu.</summary>
    /// <param name="format">Biçim.</param>
    /// <returns>Kabul ediliyorsa <c>true</c>.</returns>
    protected abstract bool IsSupported(WavFormat format);

    private static (WavFormat Format, ArraySegment<byte> Data)? Parse(byte[] bytes)
    {
        if (bytes.Length < 12 || !Tag(bytes, 0, "RIFF") || !Tag(bytes, 8, "WAVE"))
        {
            return null;
        }

        WavFormat? format = null;
        var pos = 12;
        while (pos + 8 <= bytes.Length)
        {
            var size = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pos + 4, 4));
            var bodyStart = pos + 8;
            var available = bytes.Length - bodyStart;
            var length = size > (uint)available ? available : (int)size;
            if (Tag(bytes, pos, "fmt ") && length >= 16)
            {
                var span = bytes.AsSpan(bodyStart, 16);
                format = new WavFormat(
                    BinaryPrimitives.ReadUInt16LittleEndian(span[..2]),
                    BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(2, 2)),
                    (int)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4, 4)),
                    BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(14, 2)),
                    BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(12, 2)));
            }
            else if (Tag(bytes, pos, "data") && format is not null)
            {
                var usable = length - (length % Math.Max(1, (int)format.Value.BlockAlign));
                return (format.Value, new ArraySegment<byte>(bytes, bodyStart, usable));
            }

            pos = bodyStart + length + (length % 2);
        }

        return null;
    }

    private static bool Tag(byte[] bytes, int offset, string tag) =>
        bytes.Length >= offset + 4 && bytes[offset] == tag[0] && bytes[offset + 1] == tag[1] && bytes[offset + 2] == tag[2] && bytes[offset + 3] == tag[3];

    private static byte[] BuildWav(WavFormat fmt, byte[] data)
    {
        var result = new byte[44 + data.Length];
        var span = result.AsSpan();
        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), (uint)(36 + data.Length));
        "WAVE"u8.CopyTo(span[8..]);
        "fmt "u8.CopyTo(span[12..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16, 4), 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(20, 2), fmt.FormatTag);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(22, 2), fmt.Channels);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(24, 4), (uint)fmt.SampleRate);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(28, 4), (uint)(fmt.SampleRate * fmt.BlockAlign));
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(32, 2), fmt.BlockAlign);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(34, 2), fmt.BitsPerSample);
        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(40, 4), (uint)data.Length);
        data.CopyTo(span[44..]);
        return result;
    }
}
