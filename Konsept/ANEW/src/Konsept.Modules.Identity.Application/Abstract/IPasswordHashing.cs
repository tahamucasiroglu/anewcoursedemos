namespace Konsept.Modules.Identity.Application.Abstract;

/// <summary>Şifre hash'leme (düz metin asla saklanmaz; docs/security.md, H-1/H-2).</summary>
public interface IPasswordHashing
{
    /// <summary>Şifreyi hash'ler.</summary>
    /// <param name="password">Şifre.</param>
    /// <returns>Hash (tuz ve parametrelerle birlikte).</returns>
    string Hash(string password);

    /// <summary>Şifre hash'le eşleşiyor mu.</summary>
    /// <param name="hash">Saklanan hash.</param>
    /// <param name="password">Girilen şifre.</param>
    /// <returns>Eşleşiyorsa <c>true</c>.</returns>
    bool Verify(string hash, string password);
}
