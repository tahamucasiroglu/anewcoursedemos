using OkulYonetim.Tests.Integration.Identity;

namespace OkulYonetim.Tests.Integration.Attendance;

/// <summary>
/// Spec 0010 testlerinin hazır sınıfı: okul (kurucu oturumuyla), bugünü kapsayan dönem, seviye, Merkez şubesinde bir sınıf ve
/// o sınıfa atanmış öğretmen (kişi kimliği, e-postası ve okul oturumu).
/// </summary>
public sealed record AttendanceClass(SchoolSeed School, string Founder, Guid TermId, Guid LevelId, Guid ClassroomId, Guid TeacherId, string TeacherEmail, string Teacher);
