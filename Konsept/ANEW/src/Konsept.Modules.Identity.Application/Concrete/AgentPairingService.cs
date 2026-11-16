using System.Security.Cryptography;
using Konsept.Modules.Identity.Application.Base;
using Microsoft.Extensions.Logging;

namespace Konsept.Modules.Identity.Application.Concrete;

/// <summary>Eşleştirme servisi; kod karakterleri <see cref="RandomNumberGenerator"/>'dan, olay hatası Warning olarak loglanır.</summary>
/// <param name="dependencies">Bağımlılıklar.</param>
/// <param name="logger">Teknik log (kod, sır ve belirteç asla yazılmaz).</param>
public sealed partial class AgentPairingService(AgentPairingDependencies dependencies, ILogger<AgentPairingService> logger)
    : AgentPairingServiceBase(dependencies)
{
    /// <inheritdoc />
    protected override int NextIndex(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);

    /// <inheritdoc />
    protected override void OnEventWriteFailed(Exception exception) => LogEventWriteFailed(logger, exception);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Warning, Message = "Ajan eşleştirme olayı yazılamadı; eşleştirme etkilenmedi.")]
    private static partial void LogEventWriteFailed(ILogger logger, Exception exception);
}
