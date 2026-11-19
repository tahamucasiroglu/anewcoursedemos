namespace OkulYonetim.Application.Ports;

/// <summary>
/// Şifre özeti (tek yönlü, tuzlu, yavaş — PBKDF2). Düz şifre hiçbir yerde saklanmaz ve loglanmaz (S3).
/// <c>Verify</c> özet boşsa da aynı sürede çalışır (olmayan kullanıcı için zamanlama farkı oluşmasın).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string? passwordHash, string password);
}
