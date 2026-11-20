namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>Yönetim sınıf listesi: gösterilen dönem (istenmediyse güncel, yoksa en yeni dönem; dönem yoksa boş) ve sınıflar.</summary>
public sealed record ClassroomAdminList(Guid? TermId, IReadOnlyList<ClassroomAdminView> Items);
