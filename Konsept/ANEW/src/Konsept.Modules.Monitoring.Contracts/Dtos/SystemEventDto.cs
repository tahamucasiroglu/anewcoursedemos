using Konsept.Modules.Monitoring.Contracts.Enums;

namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Kaydedilmiş bir olay.
/// </summary>
/// <param name="Id">Olay kimliği (UUIDv7).</param>
/// <param name="Type">Olay türü.</param>
/// <param name="Level">Olay seviyesi.</param>
/// <param name="Message">Türkçe metin.</param>
/// <param name="OccurredAt">Olay zamanı (UTC).</param>
/// <param name="UserId">Olayın ait olduğu kullanıcı; sistem olayında <c>null</c>.</param>
/// <param name="StartedAt">Süreli işlemin başlangıcı (UTC); süresizse <c>null</c>.</param>
/// <param name="EndedAt">Süreli işlemin bitişi (UTC); süresizse <c>null</c>.</param>
/// <param name="ProfileName">Profil etiketi; profile ait değilse <c>null</c>.</param>
public sealed record SystemEventDto(
    Guid Id,
    string Type,
    SystemEventLevel Level,
    string Message,
    DateTimeOffset OccurredAt,
    Guid? UserId,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? EndedAt = null,
    string? ProfileName = null);
