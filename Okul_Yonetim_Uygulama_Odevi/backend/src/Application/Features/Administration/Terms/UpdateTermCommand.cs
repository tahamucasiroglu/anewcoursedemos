using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Terms;

/// <summary>Dönemi düzenle (ad, tarihler). Takvim kayıtlarından birini yeni tarihlerin dışında bırakacaksa reddedilir.</summary>
public sealed record UpdateTermCommand(Guid Id, string Name, DateOnly? StartDate, DateOnly? EndDate) : IRequest<TermAdminView>, ILoggableRequest;
