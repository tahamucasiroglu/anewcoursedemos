using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Agents.Infrastructure.Logging.Concrete;

/// <summary>
/// Ajan günlük kayıtlarını hem API'nin Serilog günlüğüne (<see cref="LoggingAgentLogSink"/>) hem de kullanıcıya görünen işlem günlüğüne
/// (Diagnostics <see cref="IActivityLogger"/>; alt Günlük şeridi + yönetim paneli) yazar. <see cref="LogLevelKind.Debug"/> kayıtlar yalnızca
/// Serilog'a gider. Kayıt sahibi ajanın kullanıcısıdır; ayrıntıya cihaz, iş, kategori ve istisna metni eklenir.
/// </summary>
public sealed class ActivityAgentLogSink : AgentLogSinkBase
{
    private readonly LoggingAgentLogSink _technical;
    private readonly IActivityLogger _activity;

    /// <summary>Oluşturur.</summary>
    /// <param name="technical">Serilog alıcısı.</param>
    /// <param name="activity">Kullanıcıya görünen işlem günlüğü.</param>
    public ActivityAgentLogSink(LoggingAgentLogSink technical, IActivityLogger activity)
    {
        _technical = technical ?? throw new ArgumentNullException(nameof(technical));
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Kaydı yazar.</summary>
    /// <param name="agent">Ajan bağlamı.</param>
    /// <param name="entry">Kayıt.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Görev.</returns>
    public override async Task WriteAsync(AgentCallContext agent, AgentLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(entry);
        await _technical.WriteAsync(agent, entry, cancellationToken);
        if (entry.Level == LogLevelKind.Debug || string.IsNullOrWhiteSpace(entry.Message))
        {
            return;
        }

        var details = new
        {
            category = string.IsNullOrWhiteSpace(entry.Category) ? null : entry.Category.Trim(),
            deviceId = agent.DeviceId,
            jobId = entry.JobId,
            timestampUtc = entry.TimestampUtc == default ? (DateTimeOffset?)null : entry.TimestampUtc,
            exception = entry.Exception,
        };
        await _activity.LogAsync(entry.Level, ActivitySources.Agent, entry.Message, details, userId: agent.UserId, cancellationToken: cancellationToken);
    }
}
