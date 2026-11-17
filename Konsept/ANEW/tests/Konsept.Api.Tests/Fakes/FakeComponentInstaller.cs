using Konsept.Agent.Application.Abstract;
using Konsept.Modules.AgentGateway.Contracts.Dtos;

namespace Konsept.Api.Tests.Fakes;

/// <summary>Ajanın eksik bileşen kurucusunun sahtesi (spec 0027): listede yalnızca verilen bileşenler; kurulum istenenleri kurulu listesine ekler.</summary>
/// <param name="catalog">Kurulabilir bileşenler.</param>
/// <param name="installed">Kurulu bileşen listesi (ajanın kayıtta/bildirimde gönderdiği).</param>
public sealed class FakeComponentInstaller(IReadOnlyList<AgentComponentInfo> catalog, List<string> installed) : IComponentInstaller
{
    private readonly Lock _gate = new();

    /// <summary>Gets alınan kurulum istekleri.</summary>
    public List<IReadOnlyList<string>> Requests { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<AgentComponentInfo> Describe(IEnumerable<string> componentIds) =>
        [.. catalog.Where(c => componentIds.Contains(c.Id))];

    /// <inheritdoc />
    public Task<AgentInstallReply> InstallAsync(AgentInstallRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_gate)
        {
            Requests.Add(request.Components);
            installed.AddRange(request.Components);
        }

        return Task.FromResult(new AgentInstallReply(true, []));
    }
}
