using Konsept.Modules.AgentGateway.Application.Base;
using Konsept.Modules.Monitoring.Contracts.Abstract;
using Konsept.Modules.Monitoring.Contracts.Dtos;

namespace Konsept.Modules.AgentGateway.Application.Concrete;

/// <summary>Ajan hata bildirimi: Monitoring.Contracts <see cref="IErrorReportSubmitter"/> üzerinden.</summary>
/// <param name="submitter">Monitoring'in bildirim kapısı.</param>
public sealed class AgentErrorReportService(IErrorReportSubmitter submitter) : AgentErrorReportServiceBase
{
    /// <inheritdoc />
    protected override Task<ErrorReportSubmission> SubmitAsync(Guid ownerUserId, AgentErrorReportEntry entry, CancellationToken cancellationToken) =>
        submitter.SubmitFromAgentAsync(ownerUserId, entry, cancellationToken);
}
