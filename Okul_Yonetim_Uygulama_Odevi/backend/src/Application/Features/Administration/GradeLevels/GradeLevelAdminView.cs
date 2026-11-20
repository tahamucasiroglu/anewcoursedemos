namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>Yönetim ekranındaki sınıf seviyesi: ad, sıra numarası ve durum (pasifler de listelenir).</summary>
public sealed record GradeLevelAdminView(Guid Id, string Name, int SortOrder, bool IsActive);
