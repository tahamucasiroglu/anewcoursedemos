using TahaKonsept.Modules.Providers.Application.Models;
using TahaKonsept.Modules.Providers.Infrastructure.Media.Common;

namespace TahaKonsept.Modules.Providers.Tests.Media;

/// <summary>
/// Görsel/ses testlerinin ortak verileri: sahte PNG, gerçek kare başlıklı MP3, ham PCM, istekler ve sıfır beklemeli zamanlama ayarı.
/// </summary>
public static class MediaTestData
{
    /// <summary>Sorgu beklemesi olmayan ayar.</summary>
    public static MediaModelOptions NoWait { get; } = new(TimeSpan.Zero, TimeSpan.FromSeconds(30));

    /// <summary>PNG imzalı küçük sahte görsel.</summary>
    public static byte[] Png { get; } = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    /// <summary>Base64 PNG.</summary>
    public static string PngBase64 => Convert.ToBase64String(Png);

    /// <summary>Görsel isteği (1080×1920).</summary>
    /// <param name="count">Adet.</param>
    public static ImageRequest Image(int count = 1) => new()
    {
        Context = new CallContext(Guid.CreateVersion7(), "image"),
        Prompt = "İstanbul'da gün batımı, sinematik",
        Width = 1080,
        Height = 1920,
        Seed = 42,
        Count = count,
    };

    /// <summary>Ses isteği.</summary>
    /// <param name="voice">Ses.</param>
    /// <param name="format">Biçim.</param>
    public static SpeechRequest Speech(string? voice = "v1", string format = "mp3") => new()
    {
        Context = new CallContext(Guid.CreateVersion7(), "speech"),
        Text = "Bin dört yüz elli üç yılında İstanbul fethedildi.",
        Voice = voice,
        Format = format,
    };

    /// <summary>MPEG-1 Layer III, 128 kbps, 44.1 kHz, dolgusuz karelerden MP3 (kare başı 417 bayt, 1152 örnek).</summary>
    /// <param name="frames">Kare sayısı.</param>
    public static byte[] Mp3(int frames)
    {
        var data = new byte[frames * 417];
        for (var i = 0; i < frames; i++)
        {
            data[i * 417] = 0xFF;
            data[(i * 417) + 1] = 0xFB;
            data[(i * 417) + 2] = 0x90;
            data[(i * 417) + 3] = 0x00;
        }

        return data;
    }

    /// <summary>Sessiz 16 bit mono PCM.</summary>
    /// <param name="seconds">Süre.</param>
    /// <param name="rate">Örnekleme hızı.</param>
    public static byte[] Pcm(int seconds, int rate) => new byte[seconds * rate * 2];
}
