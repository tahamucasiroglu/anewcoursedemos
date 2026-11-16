using Konsept.Modules.AgentGateway.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Dtos;
using Konsept.Modules.Monitoring.Contracts.Enums;

namespace Konsept.Modules.AgentGateway.Application.Base;

/// <summary>
/// Ajan hata bildirimi kuralı (spec 0022 Req 8): bildirim başka bir hesapta oluşturulduysa (hesap sahipten farklı) kaydedilmez; eşleşme
/// öncesi oluşan bildirim (hesap yok) sahibin adına gider. Kayıt ve sınır Monitoring'dedir (alt sınıf).
/// </summary>
public abstract class AgentErrorReportServiceBase : IAgentErrorReportService
{
    /// <inheritdoc />
    public async Task<AgentErrorReportReply> ReportAsync(Guid ownerUserId, AgentErrorReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AccountId is { } account && account != ownerUserId)
        {
            return new AgentErrorReportReply(AgentErrorReportResult.OtherAccount);
        }

        var submission = await SubmitAsync(
            ownerUserId,
            new AgentErrorReportEntry(request.Summary, request.AgentVersion, request.OperatingSystem, request.Component, request.Step, request.LogLines),
            cancellationToken).ConfigureAwait(false);
        return submission.Outcome switch
        {
            ErrorReportOutcome.Accepted => new AgentErrorReportReply(AgentErrorReportResult.Sent, submission.Number),
            ErrorReportOutcome.RateLimited => new AgentErrorReportReply(AgentErrorReportResult.RateLimited),
            ErrorReportOutcome.OtherAccount => new AgentErrorReportReply(AgentErrorReportResult.OtherAccount),
            _ => new AgentErrorReportReply(AgentErrorReportResult.Invalid),
        };
    }

    /// <summary>Bildirimi Monitoring'e kaydettirir.</summary>
    /// <param name="ownerUserId">Sahip.</param>
    /// <param name="entry">Bildirim.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Sonuç.</returns>
    protected abstract Task<ErrorReportSubmission> SubmitAsync(Guid ownerUserId, AgentErrorReportEntry entry, CancellationToken cancellationToken);
}
