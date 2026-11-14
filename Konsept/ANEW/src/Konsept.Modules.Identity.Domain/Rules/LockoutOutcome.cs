namespace Konsept.Modules.Identity.Domain.Rules;

/// <summary>Bir yanlış şifre denemesinin sonucu (<see cref="LockoutPolicy.Evaluate"/>).</summary>
/// <param name="Applied">Deneme sayıldı mı (kilitliyken sayılmaz).</param>
/// <param name="FailedCount">Denemeden sonraki yanlış deneme sayısı (kilitlenince 0).</param>
/// <param name="LockedUntil">Denemeden sonraki kilit zamanı.</param>
public sealed record LockoutOutcome(bool Applied, int FailedCount, DateTimeOffset? LockedUntil);
