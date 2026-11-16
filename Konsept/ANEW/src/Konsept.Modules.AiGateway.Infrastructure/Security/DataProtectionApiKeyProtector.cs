using System.Security.Cryptography;
using Konsept.Modules.AiGateway.Application.Abstract;
using Microsoft.AspNetCore.DataProtection;

namespace Konsept.Modules.AiGateway.Infrastructure.Security;

/// <summary>
/// API key'leri ASP.NET Core Data Protection ile şifreler (docs/security.md, H-2). Anahtar halkası Api'nin kalıcı klasöründedir
/// (<c>DataProtection:KeysDirectory</c>); halka kaybolur ya da değişirse değer çözülemez ve satır "Key okunamıyor" olur
/// (spec 0004 Req 12). Çözülen değer bu sınıftan dışarı verilmez.
/// </summary>
/// <param name="provider">Data Protection sağlayıcısı.</param>
public sealed class DataProtectionApiKeyProtector(IDataProtectionProvider provider) : IApiKeyProtector
{
    /// <summary>Data Protection amacı (farklı amaçla şifrelenmiş değerle karışmaz).</summary>
    public const string Purpose = "Konsept.AiGateway.ApiKeys.v1";

    private readonly IDataProtector _protector = (provider ?? throw new ArgumentNullException(nameof(provider))).CreateProtector(Purpose);

    /// <inheritdoc />
    public string Protect(string key) => _protector.Protect(key);

    /// <inheritdoc />
    public bool CanUnprotect(string ciphertext) => TryUnprotect(ciphertext) is not null;

    /// <inheritdoc />
    public string? TryUnprotect(string ciphertext)
    {
        try
        {
            return _protector.Unprotect(ciphertext);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
