namespace Konsept.Agent.Domain.Enums;

/// <summary>Ajandaki "Bildir" sonucunun durumu (spec 0022 UX: ajan hata uyarısı durumları).</summary>
public enum ReportOutcome
{
    /// <summary>Gönderildi.</summary>
    Sent = 0,

    /// <summary>Bağlantı ya da eşleşme yok: bekletiliyor, bağlantı kurulunca gönderilecek.</summary>
    Queued = 1,

    /// <summary>Bildirim başka bir hesapta oluşturuldu; gönderilmedi.</summary>
    OtherAccount = 2,

    /// <summary>Saatlik sınır aşıldı; bekletmeye alınmadı.</summary>
    RateLimited = 3,

    /// <summary>Sunucu içeriği kabul etmedi; bekletmeye alınmadı.</summary>
    Rejected = 4,
}
