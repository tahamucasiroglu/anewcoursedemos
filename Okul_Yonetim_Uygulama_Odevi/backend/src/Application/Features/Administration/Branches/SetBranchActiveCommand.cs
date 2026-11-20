using NArchitecture.Core.Mediator;
using NArchitecture.Core.Application.Pipelines.Logging;

namespace OkulYonetim.Application.Features.Administration.Branches;

/// <summary>Şubeyi pasifleştir (<c>Active=false</c>) ya da yeniden etkinleştir. Okulun son etkin şubesi pasifleştirilemez.</summary>
public sealed record SetBranchActiveCommand(Guid Id, bool Active) : IRequest<BranchAdminView>, ILoggableRequest;
