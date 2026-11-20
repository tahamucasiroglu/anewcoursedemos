using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>
/// Kişi ekle (spec 0005 gereksinim 3–5): ad-soyad, e-posta, isteğe bağlı telefon ve roller. Bilinçli olarak
/// <c>ILoggableRequest</c> değildir: kişisel veri taşır (S2) ve yanıtı geçici şifre içerir (T9).
/// </summary>
public sealed record AddMemberCommand(string FullName, string Email, string? Phone, IReadOnlyList<RoleInput> Roles) : IRequest<AddMemberResult>;
