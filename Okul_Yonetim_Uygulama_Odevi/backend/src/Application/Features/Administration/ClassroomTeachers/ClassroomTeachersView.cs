namespace OkulYonetim.Application.Features.Administration.ClassroomTeachers;

/// <summary>
/// Bir sınıfın öğretmenleri (spec 0010 gereksinim 1): atanmış kişiler ve sınıfın şubesinde etkin Öğretmen olan adaylar,
/// ikisi de Türkçe ad sırasıyla.
/// </summary>
public sealed record ClassroomTeachersView(
    Guid ClassroomId,
    string ClassroomName,
    string BranchName,
    IReadOnlyList<TeacherOption> Assigned,
    IReadOnlyList<TeacherOption> Candidates);
