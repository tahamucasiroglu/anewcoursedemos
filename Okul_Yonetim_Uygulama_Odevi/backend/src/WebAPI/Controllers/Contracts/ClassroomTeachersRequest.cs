namespace OkulYonetim.WebAPI.Controllers.Contracts;

/// <summary>Sınıfın yeni öğretmen listesi (spec 0010): kişi kimlikleri; boş liste bütün atamaları kaldırır.</summary>
public sealed record ClassroomTeachersRequest(IReadOnlyList<Guid>? TeacherUserIds);
