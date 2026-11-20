namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Kurucunun öğrenci listesindeki satır (spec 0009 gereksinim 4): ad-soyad, okul numarası, şube, sınıf, veli sayısı ve
/// durum (etkin / ayrıldı). Sağlık bilgisi bilinçli olarak yoktur.
/// </summary>
public sealed record StudentRow(
    Guid Id,
    string FullName,
    string? SchoolNumber,
    Guid BranchId,
    string BranchName,
    Guid? ClassroomId,
    string? ClassroomName,
    int GuardianCount,
    bool IsActive);
