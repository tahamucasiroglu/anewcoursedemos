using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.WorkingHours;

/// <summary>Öğretmenin mesai saatleri (spec 0011 gereksinim 6); öğretmen değilse 403.</summary>
public sealed record GetMyQuietHoursQuery : IRequest<QuietHoursView>, ILoggableRequest;
