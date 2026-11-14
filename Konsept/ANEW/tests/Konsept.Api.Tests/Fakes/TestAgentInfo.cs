using Konsept.Agent.Application.Abstract;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Testlerde sabit ajan bilgisi.
/// </summary>
public sealed class TestAgentInfo : IAgentInfoProvider
{
    /// <inheritdoc />
    public string MachineName => "TEST-PC";

    /// <inheritdoc />
    public string Version => "0.1.0";
}
