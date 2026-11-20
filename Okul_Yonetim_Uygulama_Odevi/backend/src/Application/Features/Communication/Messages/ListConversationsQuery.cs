using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Kişinin konuşmaları (öğretmen ya da veli tarafı), son mesaja göre yeniden eskiye, en fazla 100.</summary>
public sealed record ListConversationsQuery : IRequest<IReadOnlyList<ConversationRow>>, ILoggableRequest;
