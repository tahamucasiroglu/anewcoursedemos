using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>Sınıfın adını ve seviyesini düzenle; dönemi ve şubesi değişmez.</summary>
public sealed record UpdateClassroomCommand(Guid Id, Guid? GradeLevelId, string Name) : IRequest<ClassroomAdminView>, ILoggableRequest;
