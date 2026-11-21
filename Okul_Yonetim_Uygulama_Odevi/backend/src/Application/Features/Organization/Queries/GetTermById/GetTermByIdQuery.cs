using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.GetTermById;

/// <summary>Tek dönem (spec 0013: değerlendirme günü sınıfın dönemi içinde mi); seçili okulda yoksa "bulunamadı". Modüller arası okuma.</summary>
public sealed record GetTermByIdQuery(Guid Id) : IRequest<TermSummary>, ILoggableRequest;
