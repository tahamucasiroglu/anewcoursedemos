namespace OkulYonetim.Application.Features.Organization;

/// <summary>Kişinin görebildiği sınıf (öğrenci ve yoklama ekranları için okuma ucu): dönem, şube, seviye ve ad.</summary>
public sealed record ClassroomSummary(Guid Id, Guid TermId, Guid BranchId, string BranchName, Guid GradeLevelId, string GradeLevelName, string Name, bool IsActive);
