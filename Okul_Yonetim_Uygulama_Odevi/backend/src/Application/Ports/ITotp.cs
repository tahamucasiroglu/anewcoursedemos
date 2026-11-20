namespace OkulYonetim.Application.Ports;

/// <summary>Zamana bağlı tek kullanımlık kod (RFC 6238: HMAC-SHA1, 6 hane, 30 sn; spec 0007).</summary>
public interface ITotp
{
    /// <summary>Yeni anahtar (Base32, doğrulayıcı uygulamaya elle yazılabilir).</summary>
    string GenerateSecret();

    /// <summary>Kod, önceki/şimdiki/sonraki dilimden biriyse doğru; eşleşen dilimi verir.</summary>
    bool Verify(string secret, string code, DateTimeOffset now, out long matchedStep);

    /// <summary>Doğrulayıcı uygulamanın kare koddan okuduğu adres.</summary>
    string ProvisioningUri(string secret, string account);
}
