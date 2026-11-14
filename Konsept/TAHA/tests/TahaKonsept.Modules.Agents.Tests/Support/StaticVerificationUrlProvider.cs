using TahaKonsept.Modules.Agents.Application.Abstract;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// Sabit doğrulama adresi (<c>http://tmpi:7110/Ayarlar#masaustu</c>).
/// </summary>
public sealed class StaticVerificationUrlProvider : IVerificationUrlProvider
{
    /// <summary>Adres.</summary>
    public const string Url = "http://tmpi:7110/Ayarlar#masaustu";

    /// <summary>Adresi döner.</summary>
    /// <returns>Adres.</returns>
    public string GetVerificationUrl() => Url;
}
