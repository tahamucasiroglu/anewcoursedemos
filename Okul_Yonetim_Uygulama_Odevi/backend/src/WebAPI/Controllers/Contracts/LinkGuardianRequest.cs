namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Veli ekleme isteği: ad-soyad, e-posta, isteğe bağlı telefon ve yakınlık (<c>Mother</c> / <c>Father</c> / <c>Other</c>).</summary>
public sealed record LinkGuardianRequest(string FullName, string Email, string? Phone, string? Relationship);
