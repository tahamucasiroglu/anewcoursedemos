using System.Globalization;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Options;

namespace TahaKonsept.Modules.Agents.Api.Security.Base;

/// <summary>
/// Doğrulama adresi sağlayıcı tabanı: ayarda mutlak <c>Agents:VerificationUrl</c> varsa onu, yoksa
/// <c>{şema}://{sunucu}:{WebPort}{VerificationPath}</c> döner; sunucu adı türetilen sınıftan (<see cref="CurrentHost"/>) gelir.
/// </summary>
public abstract class VerificationUrlProviderBase : IVerificationUrlProvider
{
    /// <summary>Oluşturur.</summary>
    /// <param name="options">Ayarlar.</param>
    protected VerificationUrlProviderBase(AgentsOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Ayarlar.</summary>
    protected AgentsOptions Options { get; }

    /// <summary>Onay sayfası adresi.</summary>
    /// <returns>Mutlak adres.</returns>
    public string GetVerificationUrl()
    {
        if (!string.IsNullOrWhiteSpace(Options.VerificationUrl))
        {
            return Options.VerificationUrl.Trim();
        }

        var (scheme, host) = CurrentHost();
        var path = string.IsNullOrWhiteSpace(Options.VerificationPath) ? "/Ayarlar#masaustu" : Options.VerificationPath.Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        var hostPart = host.Contains(':', StringComparison.Ordinal) && !host.StartsWith('[') ? "[" + host + "]" : host;
        return string.Create(CultureInfo.InvariantCulture, $"{scheme}://{hostPart}:{Options.WebPort}{path}");
    }

    /// <summary>İstek yapılan şema ve sunucu adı (port hariç).</summary>
    /// <returns>Şema ve sunucu.</returns>
    protected abstract (string Scheme, string Host) CurrentHost();
}
