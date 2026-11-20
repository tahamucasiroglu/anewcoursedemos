namespace OkulYonetim.Application.Features.Students;

/// <summary>Öğrencinin kısa bilgisi (spec 0011): kimlik, ad-soyad, şube ve sınıf (sağlık, veli ve iletişim bilgisi yok).</summary>
public sealed record StudentBrief(Guid Id, string FullName, Guid BranchId, Guid? ClassroomId);
