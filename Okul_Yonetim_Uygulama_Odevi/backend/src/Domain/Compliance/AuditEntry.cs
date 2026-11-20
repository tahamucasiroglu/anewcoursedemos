namespace OkulYonetim.Domain.Compliance;

/// <summary>
/// Audit kaydı: kim (kişi kimliği; giriş denemesinde denenen e-posta; komut işlemlerinde kaynak, ör. "komut satırı:
/// okul@tmpi"), ne, hangi kayıt, ne zaman, hangi IP; gerekirse kısa ayrıntı (yalnızca kimlik ve sayı). Yalnızca
/// eklenir — veritabanı değiştirmeyi ve silmeyi reddeder. Şifre ve oturum anahtarı asla içermez.
/// </summary>
public sealed class AuditEntry
{
    private AuditEntry(Guid id, DateTimeOffset occurredAt, AuditAction action)
    {
        this.Id = id;
        this.OccurredAt = occurredAt;
        this.Action = action;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public AuditAction Action { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string? AttemptedEmail { get; private set; }

    public string? TargetType { get; private set; }

    public Guid? TargetId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? Origin { get; private set; }

    public string? Detail { get; private set; }

    public static AuditEntry Record(
        AuditAction action,
        DateTimeOffset now,
        Guid? tenantId = null,
        Guid? actorUserId = null,
        string? attemptedEmail = null,
        string? targetType = null,
        Guid? targetId = null,
        string? ipAddress = null,
        string? origin = null,
        string? detail = null) =>
        new(Guid.CreateVersion7(now), now, action)
        {
            TenantId = tenantId,
            ActorUserId = actorUserId,
            AttemptedEmail = attemptedEmail,
            TargetType = targetType,
            TargetId = targetId,
            IpAddress = ipAddress,
            Origin = origin,
            Detail = detail,
        };
}
