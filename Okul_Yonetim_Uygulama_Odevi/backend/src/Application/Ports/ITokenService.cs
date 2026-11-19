namespace OkulYonetim.Application.Ports;

/// <summary>
/// Oturum anahtarları: kısa ömürlü imzalı erişim anahtarı (kişi + oturum + seçili kurum) ve rastgele yenileme anahtarı.
/// Yenileme anahtarı veritabanına yalnızca özeti (<c>HashRefreshToken</c>) olarak yazılır.
/// </summary>
public interface ITokenService
{
    IssuedAccessToken IssueAccessToken(Guid userId, Guid sessionId, Guid? tenantId, DateTimeOffset now);

    string CreateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
