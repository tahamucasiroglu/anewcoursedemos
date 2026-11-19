using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.GetBranchById;

/// <summary>Tek şube; seçili okulda görünmüyorsa (başka okul ya da yetkisiz şube) "bulunamadı".</summary>
public sealed record GetBranchByIdQuery(Guid Id) : IRequest<BranchSummary>, ILoggableRequest;
