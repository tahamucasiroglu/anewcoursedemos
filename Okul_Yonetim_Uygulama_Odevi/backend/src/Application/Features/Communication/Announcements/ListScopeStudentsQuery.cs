using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Announcements;

/// <summary>
/// Duyuru formunun öğrenci seçenekleri (spec 0011 gereksinim 1, review M-1): sınıfın görünür ve ayrılmamış öğrencilerinin
/// tamamı (sayfasız; okul gününden bağımsız), ada göre. Kurucu okulun her sınıfını, öğretmen yalnızca kendi sınıfını sorar
/// (başkası 404); veli ve muhasebe 403.
/// </summary>
public sealed record ListScopeStudentsQuery(Guid ClassroomId) : IRequest<IReadOnlyList<ScopeStudent>>, ILoggableRequest;
