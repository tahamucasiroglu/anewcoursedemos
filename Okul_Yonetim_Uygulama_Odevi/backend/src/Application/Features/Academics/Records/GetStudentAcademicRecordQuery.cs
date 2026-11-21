using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Records;

/// <summary>
/// Öğrenci bazlı okuma (spec 0013 gereksinim 8; spec 0014 sözleşmesi): verilen öğrenci ve tarih aralığı (uçlar dahil) için o
/// öğrencinin ödevleri ve notları. <b>Uç noktası yoktur ve çağıranın bu öğrenciye erişimini denetlemez</b> — yalnızca kurum ve
/// şube filtresi uygulanır; erişim denetimi çağırandadır (veli özeti: veli bağı; spec 0014: kendi kuralı). Plan 0013 Risks 3.
/// Öğrenci verisi taşıdığı için loglanmaz.
/// </summary>
public sealed record GetStudentAcademicRecordQuery(Guid StudentId, DateOnly From, DateOnly To) : IRequest<StudentAcademicRecord>;
