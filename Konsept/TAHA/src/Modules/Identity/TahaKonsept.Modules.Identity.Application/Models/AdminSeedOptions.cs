namespace TahaKonsept.Modules.Identity.Application.Models;

/// <summary>
/// Başlangıçta tohumlanacak yönetici hesabının ayarları: <see cref="Email"/> (aynı zamanda kullanıcı adı) ve <see cref="PasswordHash"/>
/// (önceden hesaplanmış ASP.NET Identity v3 özeti; olduğu gibi saklanır, yeniden özetlenmez). Kaynak: ortam/yapılandırma anahtarları
/// <see cref="EmailKey"/> ve <see cref="PasswordHashKey"/> (geliştirmede git dışı <c>TAHA/.env</c>, Pi'de gizli env dosyası).
/// Değerler asla loglanmaz. <see cref="IsConfigured"/> ikisi de doluysa true.
/// </summary>
public sealed record AdminSeedOptions
{
    /// <summary>E-posta anahtarı.</summary>
    public const string EmailKey = "TAHAKONSEPT_ADMIN_EMAIL";

    /// <summary>Parola özeti anahtarı.</summary>
    public const string PasswordHashKey = "TAHAKONSEPT_ADMIN_PASSWORD_HASH";

    /// <summary>Yönetici e-postası.</summary>
    public string? Email { get; init; }

    /// <summary>Önceden hesaplanmış Identity v3 parola özeti.</summary>
    public string? PasswordHash { get; init; }

    /// <summary>İki değer de dolu mu.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(PasswordHash);
}
