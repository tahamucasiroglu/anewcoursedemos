using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Application.Base;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;
using Konsept.Agent.Infrastructure.Logging;
using Serilog;

namespace Konsept.Agent.Infrastructure.Concrete;

/// <summary>
/// Kurulum motoru; teknik log Serilog'a (spec 0009 Req 22). Kurulum hataları Error seviyesinde yazılır ama
/// <see cref="ErrorAlertSink.NoAlertProperty"/> işaretini taşır: 0022 uyarısı kendiliğinden açılmaz, kullanıcı satırdaki
/// "Hatayı bildir" ile bileşen ve adımla açar (Req 14).
/// </summary>
/// <param name="catalog">Bileşen listesi.</param>
/// <param name="host">Dış dünya.</param>
/// <param name="store">Kalıcı durum.</param>
/// <param name="installRoot">Kurulum yeri.</param>
/// <param name="defaultVideoFolder">Varsayılan video klasörü.</param>
/// <param name="mirrorBase">Pi aynası adresi.</param>
/// <param name="logger">Teknik log; verilmezse yazılmaz.</param>
/// <param name="gpu">Ekran kartı (spec 0028) ya da <c>null</c>.</param>
public sealed class SetupCoordinator(
    IReadOnlyList<SetupComponent> catalog,
    ISetupHost host,
    ISetupStateStore store,
    string installRoot,
    string defaultVideoFolder,
    Uri? mirrorBase,
    ILogger? logger = null,
    GpuInfo? gpu = null) : SetupCoordinatorBase(catalog, host, store, installRoot, defaultVideoFolder, mirrorBase, gpu)
{
    private readonly ILogger _logger = (logger ?? Serilog.Core.Logger.None).ForContext<SetupCoordinator>();

    /// <inheritdoc />
    protected override void WriteLog(AgentLogLevel level, string messageTemplate, params object?[] values)
    {
        switch (level)
        {
            case AgentLogLevel.Information:
                _logger.Information(messageTemplate, values);
                break;
            case AgentLogLevel.Warning:
                _logger.Warning(messageTemplate, values);
                break;
            default:
                _logger.ForContext(ErrorAlertSink.NoAlertProperty, true).Error(messageTemplate, values);
                break;
        }
    }
}
