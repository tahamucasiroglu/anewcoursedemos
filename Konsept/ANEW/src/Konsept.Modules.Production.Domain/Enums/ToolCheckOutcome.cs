namespace Konsept.Modules.Production.Domain.Enums;

/// <summary>Hattın araç denetimi sonucu (spec 0017 Req 14, spec 0027).</summary>
public enum ToolCheckOutcome
{
    /// <summary>Gereken her şey kurulu.</summary>
    Ready = 0,

    /// <summary>Ajan koptu (iş Ajan bekleniyor olur; onay korunur).</summary>
    Dropped = 1,

    /// <summary>İş Hata olur (kurulamayan eksik, sürüm uyumsuzluğu ya da kurulum hatası).</summary>
    Failed = 2,

    /// <summary>Kurulabilir eksik var; kullanıcının indirme onayı bekleniyor.</summary>
    AwaitingApproval = 3,

    /// <summary>Kurulum sırasında Durdur.</summary>
    Stopped = 4,
}
