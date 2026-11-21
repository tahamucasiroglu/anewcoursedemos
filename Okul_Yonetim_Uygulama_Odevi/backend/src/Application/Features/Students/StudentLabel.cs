namespace OkulYonetim.Application.Features.Students;

/// <summary>Öğrencinin etiket bilgisi (spec 0015): kimlik, ad-soyad, şube kimliği ve adı, sınıf adı ve etkin mi; spec 0017: okul numarası (devamsızlık raporu). Sağlık, veli ve iletişim bilgisi yok.</summary>
public sealed record StudentLabel(Guid Id, string FullName, Guid BranchId, string BranchName, string? ClassroomName, bool IsActive, string? SchoolNumber = null);
