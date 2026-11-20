using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.GradeLevels;

/// <summary>Sınıf seviyesi ekle (spec 0008 gereksinim 3): ad (≤ 50) ve sıra (1–99) zorunlu. Kişisel veri taşımaz; loglanır.</summary>
public sealed record CreateGradeLevelCommand(string Name, int? SortOrder) : IRequest<GradeLevelAdminView>, ILoggableRequest;
