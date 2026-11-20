using NArchitecture.Core.Mediator;
using NArchitecture.Core.Application.Pipelines.Logging;

namespace OkulYonetim.Application.Features.Administration.Members;

/// <summary>Kişi kartı (yalnızca seçili okuldaki bilgiler); kişi bu okulda değilse "bulunamadı".</summary>
public sealed record GetMemberQuery(Guid UserId) : IRequest<MemberDetail>, ILoggableRequest;
