using System.Net;
using System.Net.Sockets;

namespace OkulYonetim.Infrastructure.Ai;

/// <summary>
/// Yerel yapay zekâ ayarları ("Ai" bölümü; spec 0012 C-1, C-2): metin servisi (Ollama — <c>Ai:TextGenerator:BaseUrl</c>,
/// <c>Model</c>, <c>TimeoutSeconds</c>, varsayılan 90 sn), ses servisi (<c>Ai:SpeechToText:BaseUrl</c>, <c>TimeoutSeconds</c>,
/// varsayılan 120 sn — servis 60 sn üstü sesi dökmeden döner, 60 sn'lik sesin CPU'da dökümüne pay) ve iş sırasında en fazla bekleme (<c>QueueTimeoutSeconds</c>, 30 sn). Varsayılan (Pi/üretim) adresler boştur.
/// <see cref="IsLocalAddress"/>: model çağrıları yalnızca yerel adreslere gider — <c>localhost</c>, geri döngü ya da özel ağ
/// (10/8, 172.16/12, 192.168/16) IPv4 adresi; başka bir adres yazılmışsa özellik kapalı sayılır (öğrenci verisi dışarı çıkmaz).
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public AiEndpointOptions TextGenerator { get; set; } = new() { TimeoutSeconds = 90 };

    public AiEndpointOptions SpeechToText { get; set; } = new() { TimeoutSeconds = 120 };

    public int QueueTimeoutSeconds { get; set; } = 30;

    public static bool IsLocalAddress(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)
            || !Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        if (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!IPAddress.TryParse(uri.Host, out IPAddress? address))
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        byte[] b = address.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
    }
}
