namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Öğrenci kartının sağlık bölümü: sağlık notu ve alerji (boş = sil).</summary>
public sealed record StudentHealthRequest(string? HealthNote, string? Allergy);
