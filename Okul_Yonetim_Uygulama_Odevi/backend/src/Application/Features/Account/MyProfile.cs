namespace OkulYonetim.Application.Features.Account;

/// <summary>Kişinin kendi profili (Hesabım): ad-soyad, e-posta (salt okunur), telefon.</summary>
public sealed record MyProfile(string FullName, string Email, string? Phone);
