namespace OkulYonetim.Application.Ports;

/// <summary>Üretilmiş erişim anahtarı ve geçerlilik bitişi.</summary>
public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);
