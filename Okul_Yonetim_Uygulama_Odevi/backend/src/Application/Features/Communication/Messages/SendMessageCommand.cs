using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>
/// Mesaj gönderme (spec 0011 gereksinim 5, 6): konuşma ve metin. Spec 0012: isteğe bağlı AI taslağı — öğretmenin "Gönder"i
/// taslağın onayıdır; mesaj öğretmenin son metniyle gider ve taslak aynı işlemde "onaylandı" olur. Metin taşıdığı için
/// loglanmaz (T9).
/// </summary>
public sealed record SendMessageCommand(Guid ConversationId, string? Body, Guid? AiDraftId = null) : IRequest<MessageSent>;
