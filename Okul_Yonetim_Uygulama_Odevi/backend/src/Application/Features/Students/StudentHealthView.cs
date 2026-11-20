namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Kartın sağlık bölümü: şifre çözülebildiyse <c>Available</c> true ve alanlar (boşsa <c>null</c>); şifreleme anahtarı
/// ayarlı değilse ya da veri çözülemezse <c>Available</c> false, alanlar boş — istemci "Sağlık bilgisi şu anda
/// gösterilemiyor." yazar.
/// </summary>
public sealed record StudentHealthView(bool Available, string? HealthNote, string? Allergy);
