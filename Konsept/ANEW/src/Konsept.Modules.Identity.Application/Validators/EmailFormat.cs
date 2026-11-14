using System.Text.RegularExpressions;

namespace Konsept.Modules.Identity.Application.Validators;

/// <summary>
/// E-posta biçim kontrolü: tek <c>@</c>, iki yanında boşluksuz metin, alan adında en az bir nokta ve noktayla başlayıp
/// bitmeyen bölümler. Gerçek adres doğrulaması yapılmaz (B-2).
/// </summary>
public static partial class EmailFormat
{
    /// <summary>Biçim geçerli mi.</summary>
    /// <param name="email">E-posta.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    public static bool IsValid(string? email) => email is not null && Pattern().IsMatch(email.Trim());

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
