using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.AgentGateway.Domain.Entities;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;
using Konsept.Shared.Kernel.Security;

namespace Konsept.Modules.AgentGateway.Application.Base;

/// <summary>
/// Ajan log özeti kabul kuralları: kayıtlı bağlantı, yalnızca Error (4) / Critical (5), dakikalık kota, mesaj sınırı.
/// Yazma alt sınıftadır.
/// </summary>
/// <param name="registry">Bağlı ajan kaydı.</param>
/// <param name="quota">Dakikalık kota.</param>
/// <param name="timeProvider">Saat kaynağı.</param>
public abstract class AgentLogServiceBase(IAgentConnectionRegistry registry, AgentLogQuota quota, TimeProvider timeProvider) : IAgentLogService
{
    /// <summary>Kabul edilen en uzun mesaj.</summary>
    public const int MaxMessageLength = 4000;

    /// <inheritdoc />
    public async Task<bool> ReportAsync(string connectionId, AgentLogSummary summary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(summary);
        var session = registry.Current;
        if (session is null || session.ConnectionId != connectionId
            || summary.Level is not ((int)LogEntryLevel.Error or (int)LogEntryLevel.Critical)
            || !quota.TryConsume(timeProvider.GetUtcNow()))
        {
            return false;
        }

        // Önce kaba sınır (maskeleme maliyeti sınırlı kalsın, review round 2 L-1), sonra maskele, sonra kesin kırp:
        // kesilen bir key desen eşiğinin altına düşüp parça sızdırmasın (round 1 L-2). Kaba sınırda kesilen parça
        // kesin kırpmada atılır.
        var raw = summary.Message ?? string.Empty;
        var masked = SecretMasker.Mask(raw.Length > MaxMessageLength * 2 ? raw[..(MaxMessageLength * 2)] : raw);
        var message = masked.Length > MaxMessageLength ? masked[..MaxMessageLength] : masked;
        var entry = new TechnicalLogEntry(
            summary.OccurredAt,
            (LogEntryLevel)summary.Level,
            LogEntrySource.Agent,
            message,
            AgentVersion: string.IsNullOrWhiteSpace(summary.AgentVersion) ? session.AgentVersion : summary.AgentVersion);
        await WriteAsync(entry, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>Kaydı teknik loga yazar.</summary>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    protected abstract Task WriteAsync(TechnicalLogEntry entry, CancellationToken cancellationToken);
}
