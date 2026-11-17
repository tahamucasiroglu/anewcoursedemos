namespace Konsept.Modules.Identity.Domain.Enums;

/// <summary>Ban kaydının türü (spec 0024 Req 8).</summary>
public enum BanAction
{
    /// <summary>Kullanıcı banlandı.</summary>
    Ban = 0,

    /// <summary>Ban kaldırıldı.</summary>
    Unban = 1,
}
