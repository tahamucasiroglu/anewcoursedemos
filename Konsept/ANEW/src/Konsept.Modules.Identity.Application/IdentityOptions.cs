namespace Konsept.Modules.Identity.Application;

/// <summary>
/// Identity ayarları (<c>Identity</c> bölümü). <see cref="AdminEmail"/> ilk admin hesabının e-postasıdır (spec 0011 Req 5):
/// gizli bilgi değildir; Pi'de <c>Identity__AdminEmail</c> ortam değişkeniyle verilir, repoda değer yoktur. Boşsa hiçbir
/// hesap admin olmaz.
/// </summary>
public sealed class IdentityOptions
{
    /// <summary>Yapılandırma bölümünün adı.</summary>
    public const string SectionName = "Identity";

    /// <summary>İlk admin e-postasının tam yapılandırma anahtarı.</summary>
    public const string AdminEmailConfigurationKey = SectionName + ":AdminEmail";

    /// <summary>Gets ilk admin hesabının e-postası; boşsa kimse admin değildir.</summary>
    public string? AdminEmail { get; init; }
}
