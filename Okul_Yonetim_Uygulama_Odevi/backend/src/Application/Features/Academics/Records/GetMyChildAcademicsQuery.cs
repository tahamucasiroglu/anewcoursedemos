using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Academics.Records;

/// <summary>Velinin bir çocuğu için ödev ve not özeti (spec 0013 gereksinim 7): yalnızca seçili okulda veli rolü olan ve öğrenciye bağlı kişi.</summary>
public sealed record GetMyChildAcademicsQuery(Guid StudentId) : IRequest<ChildAcademicsView>, ILoggableRequest;
