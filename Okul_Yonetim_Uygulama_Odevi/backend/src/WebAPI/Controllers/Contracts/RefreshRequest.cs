namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Oturum yenileme isteği: mobil anahtarı gövdede gönderir; boşsa (web) çerezdeki anahtar kullanılır.</summary>
public sealed record RefreshRequest(string? RefreshToken = null);
