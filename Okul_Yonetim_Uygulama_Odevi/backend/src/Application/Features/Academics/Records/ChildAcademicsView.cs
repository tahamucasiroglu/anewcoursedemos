namespace OkulYonetim.Application.Features.Academics.Records;

/// <summary>
/// Velinin çocuk özeti (spec 0013 C-7): bugün (Türkiye günü), bekleyen ödevler (teslim günü bugün ya da sonra; en yakın üstte),
/// son ödevler (teslim günü son 30 günde geçmiş; yeniden eskiye, durumlarıyla) ve etkin dönemdeki notlar (yeniden eskiye).
/// </summary>
public sealed record ChildAcademicsView(Guid StudentId, string FullName, DateOnly Today, IReadOnlyList<StudentAssignmentRecord> Pending, IReadOnlyList<StudentAssignmentRecord> Recent, IReadOnlyList<StudentGradeRecord> Grades);
