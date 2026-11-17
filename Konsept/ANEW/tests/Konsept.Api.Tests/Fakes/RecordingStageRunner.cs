using Konsept.Agent.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Ajanın yerel aşamalarının sahtesi (spec 0023): kaç komut yürütüldüğünü sayar; her komutu başarıyla cevaplar.</summary>
public sealed class RecordingStageRunner : IStageRunner
{
    /// <summary>Gets yürütülen komut sayısı (araç denetimi + aşama).</summary>
    public int Executed => _executed;

    private int _executed;

    /// <summary>Gets or sets araç denetiminde eksik bildirilen araçlar (spec 0027 testleri).</summary>
    public IReadOnlyList<string> Missing { get; set; } = [];

    /// <inheritdoc />
    public AgentToolCheckReply CheckTools(AgentToolCheckRequest request)
    {
        Interlocked.Increment(ref _executed);
        return new AgentToolCheckReply([.. Missing]);
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
