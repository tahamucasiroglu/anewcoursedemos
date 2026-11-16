namespace Konsept.Modules.Profiles.Domain.Enums;

/// <summary>Profil alan kuralı ihlali (spec 0003 Req 2, 6).</summary>
public enum ProfileIssue
{
    /// <summary>Kanal adı boş ya da yalnızca boşluk.</summary>
    NameRequired,

    /// <summary>Kanal adı 80 karakterden uzun.</summary>
    NameTooLong,

    /// <summary>Video dili listede yok.</summary>
    LanguageInvalid,

    /// <summary>Biçim listede yok.</summary>
    FormatInvalid,

    /// <summary>Hedef süre 15 sn – 15 dk dışında ya da yok.</summary>
    DurationOutOfRange,

    /// <summary>Kanal içeriği boş ya da yalnızca boşluk.</summary>
    ContentRequired,

    /// <summary>Kanal içeriği 2000 karakterden uzun.</summary>
    ContentTooLong,
}
