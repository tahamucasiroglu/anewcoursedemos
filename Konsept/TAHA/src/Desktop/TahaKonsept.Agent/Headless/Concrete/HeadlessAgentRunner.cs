using Microsoft.Extensions.Logging;
using TahaKonsept.Agent.Core.Auth.Abstract;
using TahaKonsept.Agent.Core.Configuration.Abstract;
using TahaKonsept.Agent.Core.Hosting.Abstract;
using TahaKonsept.Agent.Headless.Base;

namespace TahaKonsept.Agent.Headless.Concrete;

/// <summary>
/// <see cref="HeadlessAgentRunnerBase"/>'in somut hâli (DI ile oluşturulur; <c>--headless</c> komut satırında Program.Main çağırır).
/// </summary>
public sealed class HeadlessAgentRunner : HeadlessAgentRunnerBase
{
    /// <summary>Oluşturur.</summary>
    /// <param name="host">Ajan çekirdeği.</param>
    /// <param name="auth">Oturum servisi.</param>
    /// <param name="settings">Ajan ayarları.</param>
    /// <param name="logger">Log.</param>
    public HeadlessAgentRunner(IAgentHost host, IAgentAuthService auth, IAgentSettingsStore settings, ILogger<HeadlessAgentRunner> logger)
        : base(host, auth, settings, logger)
    {
    }
}
