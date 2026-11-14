namespace TahaKonsept.Agent.Engines.Speech.Audio;

/// <summary>
/// Bir WAV dosyasının başlık bilgisi: örnekleme hızı, kanal, bit derinliği ve ses verisi boyutu.
/// <see cref="DurationSec"/> veri boyutundan hesaplanan süredir.
/// </summary>
/// <param name="SampleRate">Örnekleme hızı (Hz).</param>
/// <param name="Channels">Kanal sayısı.</param>
/// <param name="BitsPerSample">Örnek başına bit (16, 24, 32…).</param>
/// <param name="DataBytes">"data" bölümünün bayt sayısı.</param>
public sealed record WavInfo(int SampleRate, int Channels, int BitsPerSample, long DataBytes)
{
    /// <summary>Örnek (çerçeve) sayısı (kanal başına).</summary>
    public long FrameCount => Channels <= 0 || BitsPerSample <= 0 ? 0 : DataBytes / (Channels * (BitsPerSample / 8));

    /// <summary>Süre (saniye).</summary>
    public double DurationSec => SampleRate <= 0 ? 0 : (double)FrameCount / SampleRate;
}
