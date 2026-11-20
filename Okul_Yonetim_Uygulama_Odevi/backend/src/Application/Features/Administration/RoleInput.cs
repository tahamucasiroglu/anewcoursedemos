namespace OkulYonetim.Application.Features.Administration;

/// <summary>Formdan gelen rol seçimi: rol adı (Founder, Teacher, Guardian, Accounting) ve şubeleri (kurucuda boş).</summary>
public sealed record RoleInput(string Role, IReadOnlyList<Guid> BranchIds);
