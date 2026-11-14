using Konsept.Modules.Identity.Application.Abstract;

namespace Konsept.Modules.Identity.Infrastructure.Security.Base;

/// <summary>
/// Şifre hash'lemenin ortak sözleşmesi: boş şifre hash'lenmez, boş hash hiçbir şifreyle eşleşmez. Algoritma alt sınıftadır.
/// </summary>
public abstract class PasswordHashingBase : IPasswordHashing
{
    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return HashCore(password);
    }

    /// <inheritdoc />
    public bool Verify(string hash, string password) =>
        !string.IsNullOrEmpty(hash) && !string.IsNullOrEmpty(password) && VerifyCore(hash, password);

    /// <summary>Şifreyi hash'ler.</summary>
    /// <param name="password">Şifre (boş değil).</param>
    /// <returns>Hash.</returns>
    protected abstract string HashCore(string password);

    /// <summary>Şifreyi hash'le karşılaştırır.</summary>
    /// <param name="hash">Hash (boş değil).</param>
    /// <param name="password">Şifre (boş değil).</param>
    /// <returns>Eşleşiyorsa <c>true</c>.</returns>
    protected abstract bool VerifyCore(string hash, string password);
}
