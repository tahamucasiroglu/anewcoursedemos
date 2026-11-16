namespace Konsept.Modules.AiGateway.Application.Abstract;

/// <summary>
/// API key şifrelemesi (H-2, docs/security.md: ASP.NET Core Data Protection). Çözme yalnızca AiGateway'dedir (Y-7); şifre
/// çözme anahtarları kaybolursa değer çözülemez (spec 0004 Req 12).
/// </summary>
public interface IApiKeyProtector
{
    /// <summary>Key'i şifreler.</summary>
    /// <param name="key">Düz key.</param>
    /// <returns>Şifreli değer (düz key'in hiçbir parçasını içermez).</returns>
    string Protect(string key);

    /// <summary>Şifreli değer mevcut anahtarlarla çözülebiliyor mu (çözülen değer dışarı verilmez).</summary>
    /// <param name="ciphertext">Şifreli değer.</param>
    /// <returns>Çözülebiliyorsa <c>true</c>.</returns>
    bool CanUnprotect(string ciphertext);

    /// <summary>Şifreli değeri çözer (yalnızca çağrı kapısı kullanır, spec 0013; değer loglanmaz, yanıtlara girmez).</summary>
    /// <param name="ciphertext">Şifreli değer.</param>
    /// <returns>Düz key; çözülemiyorsa <c>null</c>.</returns>
    string? TryUnprotect(string ciphertext);
}
