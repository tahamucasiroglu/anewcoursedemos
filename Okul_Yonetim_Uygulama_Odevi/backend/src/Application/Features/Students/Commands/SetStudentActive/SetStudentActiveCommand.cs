using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Commands.SetStudentActive;

/// <summary>Öğrenciyi "ayrıldı" yap (<c>Active</c> false) ya da geri al (true); öğrenci silinmez. Yalnızca kimlik taşır.</summary>
public sealed record SetStudentActiveCommand(Guid Id, bool Active) : IRequest<StudentSaved>, ILoggableRequest;
