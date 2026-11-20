namespace OkulYonetim.Application.Features.Administration;

/// <summary>Kişinin seçili okuldaki bir rolü ve şubeleri (kurucu/yönetici: şube listesi boş — okul düzeyi).</summary>
public sealed record MemberRoleView(string Role, IReadOnlyList<Guid> BranchIds);
