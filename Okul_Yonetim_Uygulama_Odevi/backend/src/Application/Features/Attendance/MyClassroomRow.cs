namespace OkulYonetim.Application.Features.Attendance;

/// <summary>"Sınıflarım" satırı (spec 0010 gereksinim 2): sınıf, şube ve bugünkü yoklamanın alınıp alınmadığı.</summary>
public sealed record MyClassroomRow(Guid Id, string Name, string BranchName, bool TakenToday);
