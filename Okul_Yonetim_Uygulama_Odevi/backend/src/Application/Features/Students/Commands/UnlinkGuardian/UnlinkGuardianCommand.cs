using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Students.Commands.UnlinkGuardian;

/// <summary>Veli bağını kaldır (spec 0009 gereksinim 7): kişinin hesabı, rolleri ve diğer bağları değişmez. Yalnızca kimlik taşır.</summary>
public sealed record UnlinkGuardianCommand(Guid StudentId, Guid GuardianUserId) : IRequest<StudentSaved>, ILoggableRequest;
