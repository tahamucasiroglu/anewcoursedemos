namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Panelden okul ekle: okul adı, ilk şube adı, kurucu ad-soyad ve e-posta.</summary>
public sealed record CreateSchoolRequest(string? SchoolName, string? BranchName, string? FounderName, string? FounderEmail);
