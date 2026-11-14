namespace TahaKonsept.Agent.Core.Audio;

/// <summary>WAV dosyasının biçim ve veri bölümü bilgisi.</summary>
/// <param name="FormatTag">Biçim etiketi (1 = PCM, 3 = float, 0xFFFE = extensible).</param>
/// <param name="Channels">Kanal sayısı.</param>
/// <param name="SampleRate">Örnekleme hızı (Hz).</param>
/// <param name="BitsPerSample">Örnek başına bit.</param>
/// <param name="BlockAlign">Kare başına bayt.</param>
/// <param name="FormatChunk">Ham <c>fmt </c> bölümü içeriği (başlıksız).</param>
/// <param name="DataOffset">Ses verisinin dosyadaki başlangıcı.</param>
/// <param name="DataLength">Ses verisinin uzunluğu (bayt).</param>
public sealed record WavInfo(
    int FormatTag,
    int Channels,
    int SampleRate,
    int BitsPerSample,
    int BlockAlign,
    byte[] FormatChunk,
    long DataOffset,
    long DataLength)
{
    /// <summary>Süre (sn).</summary>
    public double DurationSec => SampleRate > 0 && BlockAlign > 0 ? (double)DataLength / ((long)SampleRate * BlockAlign) : 0d;

    /// <summary>İki dosyanın ham veri olarak art arda eklenebilir olup olmadığı.</summary>
    /// <param name="other">Diğer dosya.</param>
    /// <returns>Biçimler aynıysa <c>true</c>.</returns>
    public bool IsCompatibleWith(WavInfo other) =>
        other is not null
        && FormatTag == other.FormatTag
        && Channels == other.Channels
        && SampleRate == other.SampleRate
        && BitsPerSample == other.BitsPerSample
        && BlockAlign == other.BlockAlign;
}
