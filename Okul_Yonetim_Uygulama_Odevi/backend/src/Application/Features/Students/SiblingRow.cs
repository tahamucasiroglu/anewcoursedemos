namespace OkulYonetim.Application.Features.Students;

/// <summary>Kardeş satırı (aynı veliye bağlı başka öğrenci, spec 0009 C-4): ad-soyad, şube, sınıf ve durum.</summary>
public sealed record SiblingRow(Guid Id, string FullName, string BranchName, string? ClassroomName, bool IsActive);
