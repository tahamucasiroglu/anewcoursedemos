using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Sahte işlem günlüğünün yakaladığı tek kayıt.
/// </summary>
/// <param name="Level">Seviye.</param>
/// <param name="Source">Kaynak.</param>
/// <param name="Message">Mesaj.</param>
/// <param name="DurationMs">Süre.</param>
/// <param name="UserId">Kullanıcı.</param>
public sealed record RecordedActivity(LogLevelKind Level, string Source, string Message, long? DurationMs, Guid? UserId);
