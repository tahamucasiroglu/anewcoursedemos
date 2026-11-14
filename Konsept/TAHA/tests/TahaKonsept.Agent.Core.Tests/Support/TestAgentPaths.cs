using TahaKonsept.Agent.Core.Paths.Base;

namespace TahaKonsept.Agent.Core.Tests.Support;

/// <summary>Verilen geçici kök altında ajan klasörleri.</summary>
public sealed class TestAgentPaths : AgentPathsBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="root">Kök.</param>
    public TestAgentPaths(string root)
        : base(root)
    {
    }
}
