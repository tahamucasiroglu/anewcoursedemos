using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Organization.Queries.GetClassroomById;

/// <summary>Tek sınıf; görünmeyen (başka okul, atanmamış ya da pasif şube) sınıf "bulunamadı".</summary>
public sealed record GetClassroomByIdQuery(Guid Id) : IRequest<ClassroomSummary>, ILoggableRequest;
