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

    /// <summary>
    /// Gets kullanıcı rolünün kullanabileceği model kimlikleri (spec 0013 Req 2; ayar <c>Identity:ModelPermissions:User</c>, virgüllü).
    /// Boş ya da yoksa tüm katalog (<c>model.use:*</c>).
    /// </summary>
    public IReadOnlyList<string> UserModels { get; init; } = [];

    /// <summary>Gets admin rolünün kullanabileceği model kimlikleri (<c>Identity:ModelPermissions:Admin</c>); boşsa tümü.</summary>
    public IReadOnlyList<string> AdminModels { get; init; } = [];

    /// <summary>Virgüllü model listesini ayrıştırır (boşluklar kırpılır, boş öğeler atlanır).</summary>
    /// <param name="value">Ayar değeri.</param>
    /// <returns>Model kimlikleri.</returns>
    public static IReadOnlyList<string> ParseModelList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
