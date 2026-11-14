using Konsept.Agent.Application.Abstract;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// Ajan bilgisi sağlayıcılarının temeli: ham versiyonu <c>+sha</c> ekinden arındırır.
/// </summary>
public abstract class AgentInfoProviderBase : IAgentInfoProvider
{
    /// <inheritdoc />
    public abstract string MachineName { get; }

    /// <inheritdoc />
    public string Version
    {
        get
        {
            var raw = ReadRawVersion();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "0.0.0";
            }

            var plus = raw.IndexOf('+', StringComparison.Ordinal);
            return plus < 0 ? raw : raw[..plus];
        }
    }

    /// <summary>Ham versiyonu okur.</summary>
    /// <returns>Ham versiyon.</returns>
    protected abstract string? ReadRawVersion();
}
