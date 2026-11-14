namespace TahaKonsept.Shared.Contracts.Identity;

/// <summary>
/// Kullanıcının tema tercihi (JSON'da metin: <c>system</c>, <c>light</c>, <c>dark</c>).
/// </summary>
public enum ThemePreference
{
    /// <summary>İşletim sistemi tercihine uy.</summary>
    System = 0,

    /// <summary>Açık tema.</summary>
    Light = 1,

    /// <summary>Koyu tema.</summary>
    Dark = 2,
}
