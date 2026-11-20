namespace OkulYonetim.Application.Ports;

/// <summary>
/// Yapay zekâ servisi şu an kullanılamıyor (spec 0012 gereksinim 5): adres ayarlı değil, servis kapalı, zaman aşımı, hata ya da
/// boş yanıt, iş sırası dolu. <see cref="Reason"/> yalnızca kısa teknik nedendir (ör. <c>timeout</c>, <c>http-503</c>) —
/// döküm ya da metin taşımaz; loglanabilir. Kullanıcıya "Yapay zekâ şu an kullanılamıyor…" denir.
/// </summary>
public sealed class AiUnavailableException : Exception
{
    public AiUnavailableException()
        : this("unknown")
    {
    }

    public AiUnavailableException(string reason)
        : base("AI servisi kullanılamıyor: " + reason)
    {
        this.Reason = reason;
    }

    public AiUnavailableException(string reason, Exception innerException)
        : base("AI servisi kullanılamıyor: " + reason, innerException)
    {
        this.Reason = reason;
    }

    public string Reason { get; } = "unknown";
}
