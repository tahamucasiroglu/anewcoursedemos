using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.SystemHealth.Queries.GetSystemHealth;

/// <summary>Anonim "sistem durumu" sorgusu; nArchitecture log pipeline'ı tarafından loglanır.</summary>
public sealed record GetSystemHealthQuery : IRequest<SystemHealthResponse>, ILoggableRequest;
