using System.Buffers.Binary;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Test için PCM 16 bit mono WAV üretici.
/// </summary>
public static class TestWav
{
    /// <summary>Sessiz WAV üretir.</summary>
    /// <param name="seconds">Süre (sn).</param>
    /// <param name="sampleRate">Örnekleme hızı.</param>
    /// <returns>WAV baytları.</returns>
    public static byte[] Silence(double seconds, int sampleRate = 24000)
    {
        var dataLength = (int)Math.Round(seconds * sampleRate) * 2;
        var bytes = new byte[44 + dataLength];
        var span = bytes.AsSpan();
        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(4, 4), (uint)(36 + dataLength));
        "WAVE"u8.CopyTo(span[8..]);
        "fmt "u8.CopyTo(span[12..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16, 4), 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(20, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(22, 2), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(24, 4), (uint)sampleRate);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(28, 4), (uint)(sampleRate * 2));
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(32, 2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(34, 2), 16);
        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(40, 4), (uint)dataLength);
        return bytes;
    }
}
