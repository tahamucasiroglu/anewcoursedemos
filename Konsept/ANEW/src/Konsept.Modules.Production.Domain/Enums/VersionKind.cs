namespace Konsept.Modules.Production.Domain.Enums;

/// <summary>Sürüm işinin türü (spec 0020): ayar değişikliğiyle yeni sürüm · düzenlenen konseptle baştan yeniden üretim.</summary>
public enum VersionKind
{
    /// <summary>Yeni sürüm (Req 1): yalnızca etkilenen aşamalar.</summary>
    NewVersion = 0,

    /// <summary>Yeniden üret (Req 4): tüm aşamalar baştan.</summary>
    Regenerate = 1,
}
