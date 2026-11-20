using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>Takvim kaydını sil (yumuşak silme; kayıt hiçbir ekranda görünmez, audit kalır).</summary>
public sealed record DeleteCalendarEntryCommand(Guid Id) : IRequest<bool>, ILoggableRequest;
