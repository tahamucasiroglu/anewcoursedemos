using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>Sınıf seviyesini pasifleştir (<c>Active=false</c>) ya da yeniden etkinleştir (ad çakışması yeniden denetlenir).</summary>
public sealed record SetGradeLevelActiveCommand(Guid Id, bool Active) : IRequest<GradeLevelAdminView>, ILoggableRequest;
