using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Queries.ListStudentLabels;

/// <summary>
/// Verilen öğrencilerin etiket bilgisi (spec 0015 tahsilat ekranları ve hatırlatma): ad-soyad, şube, sınıf ve etkin mi — ayrılmış
/// öğrenciler de döner (borç ve cari kalır). Modüller arası okuma; uç yok — yetki çağıranda (<c>BillingGuard</c>). Öğrenci filtresi
/// uygulanır: görünmeyen öğrenci listede yer almaz.
/// </summary>
public sealed record ListStudentLabelsQuery(IReadOnlyCollection<Guid> Ids) : IRequest<IReadOnlyList<StudentLabel>>;
