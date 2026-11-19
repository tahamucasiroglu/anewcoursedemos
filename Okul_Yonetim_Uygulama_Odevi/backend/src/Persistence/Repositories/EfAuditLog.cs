using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>Audit kaydını bağlama ekler; aynı işlemdeki diğer değişikliklerle birlikte yazılır.</summary>
public sealed class EfAuditLog : IAuditLog
{
    private readonly OkulDbContext context;

    public EfAuditLog(OkulDbContext context)
    {
        this.context = context;
    }

    public void Add(AuditEntry entry) => this.context.AuditEntries.Add(entry);
}
