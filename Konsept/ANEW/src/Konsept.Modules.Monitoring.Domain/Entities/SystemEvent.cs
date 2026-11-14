using Konsept.Modules.Monitoring.Domain.Enums;

namespace Konsept.Modules.Monitoring.Domain.Entities;

/// <summary>
/// Genel olay kaydındaki bir olay (spec 0001 Req 9): tür, UTC zaman, seviye, metin ve isteğe bağlı
/// kullanıcı ayrı alanlarda tutulur; Günlük ve admin aktiviteleri bu kayıttan okur.
/// </summary>
public sealed class SystemEvent
{
    /// <summary>Tür alanının en fazla uzunluğu.</summary>
    public const int TypeMaxLength = 100;

    /// <summary>Metin alanının en fazla uzunluğu; daha uzun metin kırpılır.</summary>
    public const int MessageMaxLength = 1000;

    private SystemEvent(Guid id, string type, EventLevel level, string message, DateTimeOffset occurredAt, Guid? userId)
    {
        Id = id;
        Type = type;
        Level = level;
        Message = message;
        OccurredAt = occurredAt;
        UserId = userId;
    }

    /// <summary>Gets olay kimliği (UUIDv7).</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets olay türü (kararlı kod).</summary>
    public string Type { get; private set; }

    /// <summary>Gets seviye.</summary>
    public EventLevel Level { get; private set; }

    /// <summary>Gets Türkçe metin.</summary>
    public string Message { get; private set; }

    /// <summary>Gets olay zamanı (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Gets olayın ait olduğu kullanıcı; sistem olayında <c>null</c>.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>Yeni bir olay oluşturur.</summary>
    /// <param name="type">Olay türü; boş olamaz, en fazla <see cref="TypeMaxLength"/> karakter.</param>
    /// <param name="level">Seviye.</param>
    /// <param name="message">Metin; boş olamaz, uzunsa kırpılır.</param>
    /// <param name="occurredAt">Olay zamanı; UTC'ye çevrilir.</param>
    /// <param name="userId">İsteğe bağlı kullanıcı.</param>
    /// <returns>Yeni olay.</returns>
    public static SystemEvent Create(string type, EventLevel level, string message, DateTimeOffset occurredAt, Guid? userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (type.Length > TypeMaxLength)
        {
            throw new ArgumentException($"Olay türü en fazla {TypeMaxLength} karakter olabilir.", nameof(type));
        }

        var text = message.Length > MessageMaxLength ? message[..MessageMaxLength] : message;
        return new SystemEvent(Guid.CreateVersion7(occurredAt), type, level, text, occurredAt.ToUniversalTime(), userId);
    }
}
