using NArchitecture.Core.Mediator;
using NArchitecture.Core.Application.Pipelines.Logging;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>
/// Kişinin bu okuldaki rollerini verilen listeyle değiştir (eksikler eklenir, fazlalar alınır). Boş liste: kişinin rolü
/// kalmaz, pasif görünür. Kendi kurucu rolü ve okulun son yöneticisi korunur.
/// </summary>
public sealed record SetMemberRolesCommand(Guid UserId, IReadOnlyList<RoleInput> Roles) : IRequest<MemberDetail>, ILoggableRequest;
