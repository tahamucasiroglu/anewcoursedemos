using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Administration;

/// <summary>Yönetim işlemlerinin audit kaydı (spec 0005 gereksinim 11): kim (yönetici), ne, hangi kayıt, ne zaman, IP.</summary>
public sealed class AdminAudit
{
    private readonly IAuditLog audit;
    private readonly IClientInfo client;
    private readonly TimeProvider timeProvider;

    public AdminAudit(IAuditLog audit, IClientInfo client, TimeProvider timeProvider)
    {
        this.audit = audit;
        this.client = client;
        this.timeProvider = timeProvider;
    }

    public void Record(AuditAction action, Guid tenantId, Guid actorId, string targetType, Guid targetId) =>
        this.audit.Add(AuditEntry.Record(action, this.timeProvider.GetUtcNow(), tenantId, actorId, targetType: targetType, targetId: targetId, ipAddress: this.client.IpAddress));

    public void Record(AuditAction action, Guid tenantId, Guid actorId, string targetType, Guid targetId, string detail) =>
        this.audit.Add(AuditEntry.Record(action, this.timeProvider.GetUtcNow(), tenantId, actorId, targetType: targetType, targetId: targetId, ipAddress: this.client.IpAddress, detail: detail));
}
