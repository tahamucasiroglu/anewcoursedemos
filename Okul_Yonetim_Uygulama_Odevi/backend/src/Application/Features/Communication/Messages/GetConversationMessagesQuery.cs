using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Konuşmanın mesajları: son 50, <c>Before</c> (mesaj kimliği) verilirse ondan önceki 50; açılınca okunmamışlar okunur. Yalnızca iki taraf (başkası 404).</summary>
public sealed record GetConversationMessagesQuery(Guid Id, Guid? Before) : IRequest<ConversationThread>, ILoggableRequest;
