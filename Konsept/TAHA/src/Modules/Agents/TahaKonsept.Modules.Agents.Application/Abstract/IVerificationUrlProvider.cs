namespace TahaKonsept.Modules.Agents.Application.Abstract;

/// <summary>
/// Kullanıcının eşleme kodunu onaylayacağı web adresini verir (ayar <c>Agents:VerificationUrl</c> ya da istek yapılan sunucu + web portu).
/// </summary>
public interface IVerificationUrlProvider
{
    /// <summary>Onay sayfası adresi.</summary>
    /// <returns>Mutlak adres.</returns>
    string GetVerificationUrl();
}
