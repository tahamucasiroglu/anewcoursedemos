using NArchitecture.Core.Mediator;
using NArchitecture.Core.Application.Pipelines.Logging;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Kişiyi bu okulda pasifleştir (<c>Active=false</c>) ya da yeniden etkinleştir. Kendini ve son yöneticiyi pasifleştiremez.</summary>
public sealed record SetMemberActiveCommand(Guid UserId, bool Active) : IRequest<MemberDetail>, ILoggableRequest;
