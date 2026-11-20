using OkulYonetim.Application.Features.Administration;

namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Kişinin bu okuldaki rollerinin yeni hâli (boş liste: rolü kalmaz).</summary>
public sealed record MemberRolesRequest(IReadOnlyList<RoleInput> Roles);
