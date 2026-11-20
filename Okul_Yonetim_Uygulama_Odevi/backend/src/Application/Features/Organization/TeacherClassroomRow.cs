namespace OkulYonetim.Application.Features.Organization;

/// <summary>Öğretmenin atandığı (görünür) sınıf: ad, şube, dönemin bitiş günü ve etkinlik (spec 0010 "Sınıflarım").</summary>
public sealed record TeacherClassroomRow(Guid ClassroomId, string Name, Guid BranchId, string BranchName, Guid TermId, DateOnly TermEndDate, bool IsActive);
