namespace OkulYonetim.Application.Features.Attendance;

/// <summary>Bugün "Yok" kaydedilen öğrenci (spec 0017 devamsızlık kartı): ad-soyad, yoklamanın alındığı şube ve sınıf (sağlık, veli, iletişim yok).</summary>
public sealed record AbsentStudentRow(Guid StudentId, string FullName, string BranchName, string ClassroomName);
