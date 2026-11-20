using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Kişi kartında ad-soyad ve telefonu düzenle; e-posta değişmez (kimlik). Kişisel veri taşır, loglanmaz.</summary>
public sealed record UpdateMemberProfileCommand(Guid UserId, string FullName, string? Phone) : IRequest<MemberDetail>;
