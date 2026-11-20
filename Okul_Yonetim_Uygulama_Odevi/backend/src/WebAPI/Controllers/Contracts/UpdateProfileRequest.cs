namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Profil isteği: ad-soyad ve telefon (e-posta değişmez; gönderilse de yok sayılır).</summary>
public sealed record UpdateProfileRequest(string? FullName, string? Phone);
