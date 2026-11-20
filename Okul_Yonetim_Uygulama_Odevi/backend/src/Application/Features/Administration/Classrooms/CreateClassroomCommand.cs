using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>Sınıf aç (spec 0008 gereksinim 6): dönem, etkin şube, etkin seviye ve ad (≤ 30) zorunlu. Kişisel veri taşımaz.</summary>
public sealed record CreateClassroomCommand(Guid? TermId, Guid? BranchId, Guid? GradeLevelId, string Name) : IRequest<ClassroomAdminView>, ILoggableRequest;
