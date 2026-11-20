namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>İki adımlı doğrulama kurulumu başlat: girişte verilen bekleme anahtarı.</summary>
public sealed record TwoFactorChallengeRequest(string? Challenge);
