using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Agents.Tests.Support;

/// <summary>
/// <see cref="RecordingActivityLogger"/>'ın yakaladığı tek işlem günlüğü kaydı.
/// </summary>
/// <param name="Level">Seviye.</param>
/// <param name="Source">Kaynak.</param>
/// <param name="Message">Mesaj.</param>
/// <param name="Details">Ayrıntı.</param>
/// <param name="UserId">Sahip.</param>
public sealed record RecordedActivity(LogLevelKind Level, string Source, string Message, object? Details, Guid? UserId);
