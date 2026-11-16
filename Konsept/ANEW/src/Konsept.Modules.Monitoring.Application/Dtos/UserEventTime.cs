namespace Konsept.Modules.Monitoring.Application.Dtos;

/// <summary>Kullanıcılı bir olayın sahibi ve anı (günlük aktif kullanıcı için).</summary>
/// <param name="UserId">Kullanıcı.</param>
/// <param name="OccurredAt">An (UTC).</param>
public sealed record UserEventTime(Guid UserId, DateTimeOffset OccurredAt);
