using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.GetBranches;

/// <summary>Kişinin seçili okulda görebildiği şubeler (kurucu: tümü; diğer roller: atandıkları).</summary>
public sealed record GetBranchesQuery : IRequest<IReadOnlyList<BranchSummary>>, ILoggableRequest;
