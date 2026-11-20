using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Platform;

/// <summary>Panelden okul aç (okul + ilk şube + kurucu). Kişisel veri taşır, loglanmaz.</summary>
public sealed record CreateSchoolFromPanelCommand(string SchoolName, string BranchName, string FounderName, string FounderEmail) : IRequest<PanelSchoolResult>;
