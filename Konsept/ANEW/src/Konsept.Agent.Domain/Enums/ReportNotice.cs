namespace Konsept.Agent.Domain.Enums;

/// <summary>Bekleyen bir bildirim sonradan gönderilemediğinde kullanıcıya verilen bilgi (spec 0022 Req 8, 15).</summary>
public enum ReportNotice
{
    /// <summary>"Bu bildirim başka bir hesapta oluşturuldu, gönderilmedi".</summary>
    OtherAccount = 0,

    /// <summary>"Çok fazla bildirim gönderdiniz, biraz sonra tekrar deneyin".</summary>
    RateLimited = 1,

    /// <summary>Bekleyen bildirim gönderildi.</summary>
    Sent = 2,
}
