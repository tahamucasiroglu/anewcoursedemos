using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Seçili okulun kişileri: arama (ad/e-posta), rol, şube, durum ("active"/"inactive") süzgeci, sayfa başına 25. Arama metni kişisel veri olabilir; loglanmaz.</summary>
public sealed record ListMembersQuery(string? Search, string? Role, Guid? BranchId, string? Status, int Page) : IRequest<MemberPage>;
