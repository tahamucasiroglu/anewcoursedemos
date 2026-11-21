namespace OkulYonetim.Application.Features.Academics.Assignments;

/// <summary>
/// Teslim listesi (spec 0013 gereksinim 4): ödevin ders, başlık ve teslim günü, veren öğretmen, isteyen değiştirebilir mi
/// (<c>CanEdit</c> — yalnızca veren) ve sınıfta süren öğrencileri Türkçe ad sırasıyla durumlarıyla.
/// </summary>
public sealed record AssignmentSubmissionsView(Guid AssignmentId, string Subject, string Title, DateOnly DueDate, string CreatedByName, bool CanEdit, IReadOnlyList<SubmissionStudentRow> Students);
