using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Commands.SetStudentHealth;

/// <summary>
/// Kartın sağlık bölümünü kaydet (spec 0009 gereksinim 9): sağlık notu ve alerji (boş = sil). Şifreleme anahtarı yoksa dolu
/// değer reddedilir; diğer bilgiler bu komuttan etkilenmez. Sağlık verisi taşır; <c>ILoggableRequest</c> değildir (T9).
/// </summary>
public sealed record SetStudentHealthCommand(Guid Id, string? HealthNote, string? Allergy) : IRequest<StudentSaved>;
