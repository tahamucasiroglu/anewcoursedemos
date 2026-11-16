namespace Konsept.Modules.Monitoring.Contracts.Enums;

/// <summary>Bir hata bildirimi gönderiminin sonucu (spec 0022 Req 4, 8, 15).</summary>
public enum ErrorReportOutcome
{
    /// <summary>Kaydedildi.</summary>
    Accepted = 0,

    /// <summary>Saatlik sınır aşıldı; kayıt oluşmadı.</summary>
    RateLimited = 1,

    /// <summary>Eksik ya da geçersiz içerik; kayıt oluşmadı.</summary>
    Invalid = 2,

    /// <summary>Bildirim başka bir hesapta oluşturuldu; kayıt oluşmadı.</summary>
    OtherAccount = 3,
}
