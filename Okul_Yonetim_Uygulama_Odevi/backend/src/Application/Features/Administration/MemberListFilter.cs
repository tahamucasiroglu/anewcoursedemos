using OkulYonetim.Domain.Identity;

namespace OkulYonetim.Application.Features.Administration;

/// <summary>Kişi listesi süzgeci: ad/e-posta araması, rol, şube, durum (etkin/pasif) ve sayfa (1'den başlar).</summary>
public sealed record MemberListFilter(string? Search, Role? Role, Guid? BranchId, bool? Active, int Page, int PageSize);
