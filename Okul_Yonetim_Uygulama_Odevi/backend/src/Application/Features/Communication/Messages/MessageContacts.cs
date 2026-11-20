namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Yeni mesaj seçenekleri: veli tarafı (çocuk → öğretmenler) ve öğretmen tarafı (sınıf → öğrenci → veliler); rolü olmayan taraf boş.</summary>
public sealed record MessageContacts(IReadOnlyList<ChildTeachers> AsGuardian, IReadOnlyList<ClassroomContacts> AsTeacher);
