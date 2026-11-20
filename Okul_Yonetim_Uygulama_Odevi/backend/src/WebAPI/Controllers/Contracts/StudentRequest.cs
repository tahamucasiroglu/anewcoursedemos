namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>
/// Öğrenci ekleme/düzenleme isteği (spec 0009): ad-soyad, doğum tarihi (<c>yyyy-MM-dd</c>), okul numarası, şube, sınıf;
/// eklemede ayrıca sağlık notu ve alerji. Düzenlemede şube kayıttakiyle aynı olmalı, sağlık alanları kullanılmaz (ayrı uç).
/// </summary>
public sealed record StudentRequest(
    string FullName,
    DateOnly? BirthDate,
    string? SchoolNumber,
    Guid? BranchId,
    Guid? ClassroomId,
    string? HealthNote,
    string? Allergy);
