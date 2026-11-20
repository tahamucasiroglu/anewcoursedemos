namespace OkulYonetim.Application.Features.Students;

/// <summary>Kurucunun kartındaki veli satırı: kişi kimliği, ad-soyad, e-posta, telefon ve yakınlık (Mother/Father/Other).</summary>
public sealed record StudentGuardianView(Guid UserId, string FullName, string Email, string? Phone, string Relationship);
