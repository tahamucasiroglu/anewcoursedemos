using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Queries.ListStudentBriefs;

/// <summary>
/// Görünür ve ayrılmamış öğrencilerin kısa bilgisi (spec 0011 duyuru kapsamı ve mesaj ilişkisi): şube ve/veya sınıf ve/veya
/// kimlik listesiyle süzülür (hepsi boşsa okulun bütün görünür öğrencileri). Modüller arası okuma; uç yok — yetki çağıranda
/// (iletişim kapısı). Öğrenci filtresi uygulanır: kurucu bütün şubeleri, diğerleri yalnızca kendi şubelerini görür.
/// </summary>
public sealed record ListStudentBriefsQuery(Guid? BranchId = null, Guid? ClassroomId = null, IReadOnlyCollection<Guid>? Ids = null) : IRequest<IReadOnlyList<StudentBrief>>;
