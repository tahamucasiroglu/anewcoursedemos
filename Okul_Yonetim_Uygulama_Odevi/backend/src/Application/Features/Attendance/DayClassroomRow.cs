namespace OkulYonetim.Application.Features.Attendance;

/// <summary>Bugünkü yoklama özetinde bir sınıf (spec 0017 devamsızlık kartı): şube, sınıf, atanmış öğretmen adları, öğrenci sayısı ve yoklaması alındı mı.</summary>
public sealed record DayClassroomRow(Guid ClassroomId, string BranchName, string ClassroomName, IReadOnlyList<string> TeacherNames, int StudentCount, bool Taken);
