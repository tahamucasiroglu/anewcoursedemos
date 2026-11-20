namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>
/// Yönetim ekranındaki sınıf: dönem, şube (adı ve etkin mi — pasif şubenin sınıfı "şube pasif" etiketiyle), seviye
/// (adı ve sırası), sınıf adı ve durum.
/// </summary>
public sealed record ClassroomAdminView(
    Guid Id,
    Guid TermId,
    Guid BranchId,
    string BranchName,
    bool BranchIsActive,
    Guid GradeLevelId,
    string GradeLevelName,
    int GradeLevelSortOrder,
    string Name,
    bool IsActive);
