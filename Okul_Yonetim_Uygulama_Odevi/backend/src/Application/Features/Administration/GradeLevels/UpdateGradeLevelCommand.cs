using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>Sınıf seviyesini düzenle (ad, sıra).</summary>
public sealed record UpdateGradeLevelCommand(Guid Id, string Name, int? SortOrder) : IRequest<GradeLevelAdminView>, ILoggableRequest;
