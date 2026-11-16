using Konsept.Modules.Monitoring.Contracts.Enums;

namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Genel olay kaydına yazılacak yeni olay. Zamanı yazan taraf değil Monitoring belirler (UTC). Süreli işlemler başlangıç ve bitişi,
/// bir profile ait işlemler profil kimliği ve adını verir (spec 0021 Req 4, 9).
/// </summary>
/// <param name="Type">Olay türü, kararlı kod (ör. <c>agent.connected</c>).</param>
/// <param name="Level">Olay seviyesi.</param>
/// <param name="Message">Kullanıcıya gösterilecek Türkçe metin.</param>
/// <param name="UserId">Olayın ait olduğu kullanıcı; sistem olayında <c>null</c>.</param>
/// <param name="StartedAt">Süreli işlemin başlangıcı.</param>
/// <param name="EndedAt">Süreli işlemin bitişi.</param>
/// <param name="ProfileId">Olayın ait olduğu profil.</param>
/// <param name="ProfileName">Profilin adı (Günlük'te etiket).</param>
public sealed record SystemEventEntry(
    string Type,
    SystemEventLevel Level,
    string Message,
    Guid? UserId = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? EndedAt = null,
    Guid? ProfileId = null,
    string? ProfileName = null);
