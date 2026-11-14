namespace TahaKonsept.Modules.Profiles.Domain.Constants;

/// <summary>
/// Profil alanlarının sınırları (veritabanı kolon uzunlukları ve doğrulama kuralları tek yerden buradan okunur).
/// </summary>
public static class ProfileLimits
{
    /// <summary>Kanal adı en fazla uzunluk.</summary>
    public const int NameMaxLength = 120;

    /// <summary>Dil kodu en fazla uzunluk.</summary>
    public const int LanguageMaxLength = 16;

    /// <summary>Kanal içeriği en fazla uzunluk.</summary>
    public const int ChannelDescriptionMaxLength = 4000;

    /// <summary>Ton / izleyici / anlatım alanları en fazla uzunluk.</summary>
    public const int StyleFieldMaxLength = 200;

    /// <summary>Şablon anahtarı en fazla uzunluk.</summary>
    public const int TemplateKeyMaxLength = 64;

    /// <summary>En kısa hedef süre (saniye).</summary>
    public const int MinDurationSec = 15;

    /// <summary>En uzun hedef süre (saniye).</summary>
    public const int MaxDurationSec = 1800;

    /// <summary>Varsayılan dil.</summary>
    public const string DefaultLanguage = "tr";
}
