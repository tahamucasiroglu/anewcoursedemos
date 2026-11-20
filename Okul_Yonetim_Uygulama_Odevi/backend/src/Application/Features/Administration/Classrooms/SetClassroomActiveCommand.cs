using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Administration.Classrooms;

/// <summary>Sınıfı pasifleştir (<c>Active=false</c>) ya da yeniden etkinleştir (ad çakışması yeniden denetlenir).</summary>
public sealed record SetClassroomActiveCommand(Guid Id, bool Active) : IRequest<ClassroomAdminView>, ILoggableRequest;
