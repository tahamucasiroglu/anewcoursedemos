namespace OkulYonetim.Application.Features.Organization;

/// <summary>Bugünkü okul gününün etkin sınıfı (spec 0017 devamsızlık kartı): kimlik, şube, sınıf adı ve atanmış öğretmenlerin adları (Türkçe sıra).</summary>
public sealed record TodayClassroom(Guid Id, Guid BranchId, string BranchName, string Name, IReadOnlyList<string> TeacherNames);
