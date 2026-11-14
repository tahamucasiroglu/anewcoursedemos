using System.Reflection;
using Konsept.Agent.Application.Base;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Bilgisayar adını işletim sisteminden, versiyonu derlemenin bilgisinden (tek ürün versiyonu) okur.
/// </summary>
public sealed class AssemblyAgentInfoProvider : AgentInfoProviderBase
{
    private readonly Assembly _assembly;

    /// <summary>Initializes a new instance of the <see cref="AssemblyAgentInfoProvider"/> class.</summary>
    /// <param name="assembly">Versiyonu taşıyan derleme (ajanın giriş derlemesi).</param>
    public AssemblyAgentInfoProvider(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    /// <inheritdoc />
    public override string MachineName => Environment.MachineName;

    /// <inheritdoc />
    protected override string? ReadRawVersion() =>
        _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
