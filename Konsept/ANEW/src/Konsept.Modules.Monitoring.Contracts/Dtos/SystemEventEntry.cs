using Konsept.Modules.Monitoring.Contracts.Enums;

namespace Konsept.Modules.Monitoring.Contracts.Dtos;

/// <summary>
/// Genel olay kaydına yazılacak yeni olay. Zamanı yazan taraf değil Monitoring belirler (UTC).
/// </summary>
/// <param name="Type">Olay türü, kararlı kod (ör. <c>agent.connected</c>).</param>
/// <param name="Level">Olay seviyesi.</param>
/// <param name="Message">Kullanıcıya gösterilecek Türkçe metin.</param>
/// <param name="UserId">Olayın ait olduğu kullanıcı; sistem olayında <c>null</c>.</param>
public sealed record SystemEventEntry(string Type, SystemEventLevel Level, string Message, Guid? UserId = null);
