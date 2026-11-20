namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Kişi kartı düzenleme isteği: ad-soyad ve telefon (e-posta değişmez).</summary>
public sealed record MemberProfileRequest(string FullName, string? Phone);
