namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Velinin gördüğü salt okunur öğrenci kartı (spec 0009 C-7): ad-soyad, doğum tarihi, okul numarası, şube, sınıf, sağlık
/// bölümü ve teslim alabilecek kişiler. Diğer velilerin bilgisi ve kardeş kartları bilinçli olarak yoktur.
/// </summary>
public sealed record ChildCard(
    Guid Id,
    string FullName,
    DateOnly BirthDate,
    string? SchoolNumber,
    string BranchName,
    string? ClassroomName,
    StudentHealthView Health,
    IReadOnlyList<PickupPersonView> PickupPersons);
