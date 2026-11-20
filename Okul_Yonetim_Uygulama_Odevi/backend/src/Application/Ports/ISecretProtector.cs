namespace OkulYonetim.Application.Ports;

/// <summary>Gizli değeri veritabanında şifreli tutmak için (spec 0007: doğrulama anahtarı). Anahtar ortam ayarından gelir.</summary>
public interface ISecretProtector
{
    bool IsConfigured { get; }

    string Protect(string plaintext);

    string Unprotect(string cipher);
}
