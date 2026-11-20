namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Konuşmadaki tek mesaj (spec 0011 gereksinim 5): gönderen, metin (boş değil, en fazla <see cref="BodyMaxLength"/>) ve
/// zaman. Düzenlenmez, silinmez. Metin yalnızca iki tarafa görünür; loglanmaz ve audit'e yazılmaz.
/// </summary>
public sealed class ConversationMessage
{
    public const int BodyMaxLength = 2000;

    private ConversationMessage(Guid id, Guid tenantId, Guid conversationId, Guid senderUserId, string body, DateTimeOffset sentAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.ConversationId = conversationId;
        this.SenderUserId = senderUserId;
        this.Body = body;
        this.SentAt = sentAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid ConversationId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string Body { get; private set; }

    public DateTimeOffset SentAt { get; private set; }

    public static ConversationMessage Write(Conversation conversation, Guid senderUserId, string body, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(body);
        return new(Guid.CreateVersion7(now), conversation.TenantId, conversation.Id, senderUserId, body.Trim(), now);
    }
}
