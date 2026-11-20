using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Mesaj gönderme (spec 0011 gereksinim 5, 6): konuşma ve metin. Metin taşıdığı için loglanmaz (T9).</summary>
public sealed record SendMessageCommand(Guid ConversationId, string? Body) : IRequest<MessageSent>;
