namespace OkulYonetim.Application.Features.Students;

/// <summary>
/// Kurucunun öğrenci kartı (spec 0009): bilgiler, sağlık bölümü, veliler, kardeşler (aynı veliye bağlı öğrenciler) ve
/// teslim alabilecek kişiler. Her okunuşu audit'e geçer (kart görüntüleme; sağlık okunduysa ayrıca sağlık okuma).
/// </summary>
public sealed record StudentDetail(
    Guid Id,
    string FullName,
    DateOnly BirthDate,
    string? SchoolNumber,
    Guid BranchId,
    string BranchName,
    Guid? ClassroomId,
    string? ClassroomName,
    bool IsActive,
    StudentHealthView Health,
    IReadOnlyList<StudentGuardianView> Guardians,
    IReadOnlyList<SiblingRow> Siblings,
    IReadOnlyList<PickupPersonView> PickupPersons);
