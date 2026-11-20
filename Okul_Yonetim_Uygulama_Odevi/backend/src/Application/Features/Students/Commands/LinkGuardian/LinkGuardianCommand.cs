using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Commands.LinkGuardian;

/// <summary>
/// Öğrenciye veli bağla (spec 0009 gereksinim 6): ad-soyad, e-posta, isteğe bağlı telefon ve yakınlık (Mother / Father /
/// Other). Bilinçli olarak <c>ILoggableRequest</c> değildir: kişisel veri taşır ve yanıtı geçici şifre içerebilir (T9).
/// </summary>
public sealed record LinkGuardianCommand(Guid StudentId, string FullName, string Email, string? Phone, string? Relationship) : IRequest<LinkGuardianResult>;
