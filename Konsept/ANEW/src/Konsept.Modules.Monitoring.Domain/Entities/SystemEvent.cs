using Konsept.Modules.Monitoring.Domain.Enums;
using Konsept.Shared.Kernel.Security;

namespace Konsept.Modules.Monitoring.Domain.Entities;

/// <summary>
/// Genel olay kaydındaki bir olay (spec 0001 Req 9): tür, UTC zaman, seviye, metin ve isteğe bağlı
/// kullanıcı ayrı alanlarda tutulur; Günlük ve admin aktiviteleri bu kayıttan okur. Metin oluşturulurken
/// maskelenir (spec 0008 Req 8).
/// </summary>
public sealed class SystemEvent
{
    /// <summary>Tür alanının en fazla uzunluğu.</summary>
    public const int TypeMaxLength = 100;

    /// <summary>Metin alanının en fazla uzunluğu; daha uzun metin kırpılır.</summary>
    public const int MessageMaxLength = 1000;

    /// <summary>Profil adı alanının en fazla uzunluğu; daha uzun ad kırpılır.</summary>
    public const int ProfileNameMaxLength = 100;

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

    /// <summary>Gets süreli işlemin başlangıcı (UTC; spec 0021 Req 4); süresiz olayda <c>null</c>.</summary>
    public DateTimeOffset? StartedAt { get; private set; }

    /// <summary>Gets süreli işlemin bitişi (UTC); süresiz olayda <c>null</c>.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>Gets olayın ait olduğu profil (spec 0021 Req 9); profile ait değilse <c>null</c>.</summary>
    public Guid? ProfileId { get; private set; }

    /// <summary>Gets profilin olay yazıldığı andaki adı (etiket; maskelenmiş); profile ait değilse <c>null</c>.</summary>
    public string? ProfileName { get; private set; }

    /// <summary>Yeni bir olay oluşturur.</summary>
    /// <param name="type">Olay türü; boş olamaz, en fazla <see cref="TypeMaxLength"/> karakter.</param>
    /// <param name="level">Seviye.</param>
    /// <param name="message">Metin; boş olamaz, maskelenir, uzunsa kırpılır.</param>
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

        var masked = SecretMasker.Mask(message);
        var text = masked.Length > MessageMaxLength ? masked[..MessageMaxLength] : masked;
        return new SystemEvent(Guid.CreateVersion7(occurredAt), type, level, text, occurredAt.ToUniversalTime(), userId);
    }

    /// <summary>
    /// Süreli işlem (başlangıç–bitiş) ve profil etiketini ekler (spec 0021 Req 4, 9). Bitiş başlangıçtan önceyse ikisi yer değiştirir;
    /// yalnızca biri verilirse süre yok sayılır. Profil adı maskelenir ve kırpılır; boş ad etiketsiz demektir.
    /// </summary>
    /// <param name="startedAt">Başlangıç.</param>
    /// <param name="endedAt">Bitiş.</param>
    /// <param name="profileId">Profil.</param>
    /// <param name="profileName">Profil adı.</param>
    /// <returns>Aynı olay.</returns>
    public SystemEvent WithDetails(DateTimeOffset? startedAt, DateTimeOffset? endedAt, Guid? profileId, string? profileName)
    {
        if (startedAt is { } start && endedAt is { } end)
        {
            StartedAt = (start <= end ? start : end).ToUniversalTime();
            EndedAt = (start <= end ? end : start).ToUniversalTime();
        }

        ProfileId = profileId;
        if (!string.IsNullOrWhiteSpace(profileName))
        {
            var name = SecretMasker.Mask(profileName.Trim());
            ProfileName = name.Length > ProfileNameMaxLength ? name[..ProfileNameMaxLength] : name;
        }

        return this;
    }
}
