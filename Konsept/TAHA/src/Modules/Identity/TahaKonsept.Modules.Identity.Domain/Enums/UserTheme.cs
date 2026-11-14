namespace TahaKonsept.Modules.Identity.Domain.Enums;

/// <summary>
/// Kullanıcının kayıtlı tema tercihi (DB'de metin olarak saklanır).
/// </summary>
public enum UserTheme
{
    /// <summary>İşletim sistemine uy.</summary>
    System = 0,

    /// <summary>Açık tema.</summary>
    Light = 1,

    /// <summary>Koyu tema.</summary>
    Dark = 2,
}
