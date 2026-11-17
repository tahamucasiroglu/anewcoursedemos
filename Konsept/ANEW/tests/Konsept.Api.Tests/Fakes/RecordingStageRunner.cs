using Konsept.Agent.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Ajanın yerel aşamalarının sahtesi (spec 0023): kaç komut yürütüldüğünü sayar; her komutu başarıyla cevaplar.</summary>
public sealed class RecordingStageRunner : IStageRunner
{
    /// <summary>Gets yürütülen komut sayısı (araç denetimi + aşama).</summary>
    public int Executed => _executed;

    private int _executed;

    /// <inheritdoc />
    public AgentToolCheckReply CheckTools(AgentToolCheckRequest request)
    {
        Interlocked.Increment(ref _executed);
        return new AgentToolCheckReply([]);
    }

    /// <inheritdoc />
    public Task<AgentStageReply> RunAsync(AgentStageCommand command)
    {
        Interlocked.Increment(ref _executed);
        return Task.FromResult(new AgentStageReply(true));
    }

    /// <inheritdoc />
    public void Cancel(Guid jobId)
    {
    }
}
