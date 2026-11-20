namespace OkulYonetim.Application.Features.Students;

/// <summary>Velinin "Çocuklarım" listesindeki satır (spec 0009 gereksinim 11): ad-soyad, şube ve sınıf.</summary>
public sealed record ChildRow(Guid Id, string FullName, string BranchName, string? ClassroomName);
