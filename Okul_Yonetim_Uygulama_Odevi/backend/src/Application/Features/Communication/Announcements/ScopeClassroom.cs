namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>Duyuru kapsamı için seçilebilen sınıf: kimlik, ad ve şube adı.</summary>
public sealed record ScopeClassroom(Guid Id, string Name, string BranchName);
