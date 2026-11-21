namespace OkulYonetim.Application.Features.Ai.WeeklySummaries;

/// <summary>
/// Veliye ve öğretmene görünen dayanak satırının parçaları (spec 0014 C-5; YAML <c>ai.tr.yaml</c>): ön ek ("Dayanak: "), yoklama
/// ("{present} gün geldi{late}, {absent} gün gelmedi"), geç eki (" ({late} gün geç)"), yoklama yok, ödev ("{count} ödev"),
/// not ("{count} not") ve "öğretmen notu". Yer tutucular yalnızca sayıyla doldurulur.
/// </summary>
public sealed record WeeklySummaryBasisTexts(string Prefix, string Attendance, string Late, string NoAttendance, string Assignments, string Grades, string TeacherNote);
