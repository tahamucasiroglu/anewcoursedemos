using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Application.Base;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Concrete;

/// <summary>Ajan log özetlerini Monitoring'in teknik log yazıcısına iletir (Y-1: yalnızca Contracts).</summary>
/// <param name="registry">Bağlı ajan kaydı.</param>
/// <param name="quota">Dakikalık kota.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
/// <param name="writer">Teknik log yazıcısı.</param>
public sealed class AgentLogService(IAgentConnectionRegistry registry, AgentLogQuota quota, TimeProvider timeProvider, ITechnicalLogWriter writer)
    : AgentLogServiceBase(registry, quota, timeProvider)
{
    /// <inheritdoc />
    protected override Task WriteAsync(TechnicalLogEntry entry, CancellationToken cancellationToken) =>
        writer.WriteAsync([entry], cancellationToken);
}
