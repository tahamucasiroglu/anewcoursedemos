using System.Collections.Concurrent;
using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Rules;
using Konsept.Modules.AgentGateway.Contracts.Dtos;
using Serilog;

namespace Konsept.Agent.Infrastructure.Base;

/// <summary>
/// Yerel aşamaların ortak mantığı (spec 0017): araç denetimi kurulum durumundan (0009 <c>setup-state.json</c>), aşama başına işin iptal
/// belirteci (Durdur → <see cref="AgentStageReply.Canceled"/>), beklenmeyen hata → <see cref="AgentStageReply.Failed"/> (iç ayrıntı cevaba
/// girmez, loga yazılır). Aşamaların kendisi alt sınıftadır.
/// </summary>
/// <param name="setup">Kurulum durumu.</param>
/// <param name="logger">Teknik log.</param>
/// <param name="catalog">Çözülmüş bileşen listesi (spec 0028: bağımlılıklar ve ekran kartı gereksinimi); verilmezse yalnızca kurulu mu bakılır.</param>
/// <param name="gpu">Tespit edilen ekran kartı ya da <c>null</c>.</param>
public abstract class StageRunnerBase(ISetupStateStore setup, ILogger logger, IReadOnlyList<SetupComponent>? catalog = null, GpuInfo? gpu = null) : IStageRunner
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _running = new();

    /// <summary>Gets teknik log.</summary>
    protected ILogger Logger { get; } = (logger ?? throw new ArgumentNullException(nameof(logger))).ForContext<StageRunnerBase>();

    /// <inheritdoc />
    public AgentToolCheckReply CheckTools(AgentToolCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Eksik modelin kurulu olmayan bağımlılıkları da eksiktir; ekran kartı gereksinimi karşılanmayan model desteklenmez (spec 0028 Req 2, 6).
        var result = ComponentRules.Check(request.Tools, catalog ?? [], setup.Load().Installed.Keys.ToHashSet(StringComparer.Ordinal), gpu);
        return new AgentToolCheckReply(
            result.Missing,
            Unsupported: result.Unsupported.Count == 0
                ? null
                : [.. result.Unsupported.Select(c => new AgentUnsupportedTool(c.Id, AgentUnsupportedTool.GpuRequired, c.Gpu?.MinVramMb))]);
    }

    /// <inheritdoc />
    public async Task<AgentStageReply> RunAsync(AgentStageCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        using var cancel = new CancellationTokenSource();
        _running[command.JobId] = cancel;
        try
        {
            return command.Stage switch
            {
                AgentStageCommand.Images => await ImagesAsync(command, cancel.Token).ConfigureAwait(false),
                AgentStageCommand.Voice => await VoiceAsync(command, cancel.Token).ConfigureAwait(false),
                AgentStageCommand.Render => await RenderAsync(command, cancel.Token).ConfigureAwait(false),
                AgentStageCommand.Merge => await MergeAsync(command, cancel.Token).ConfigureAwait(false),
                _ => new AgentStageReply(false, AgentStageReply.Failed),
            };
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            Logger.Information("Üretim aşaması durduruldu: {Stage}", command.Stage);
            return new AgentStageReply(false, AgentStageReply.Canceled);
        }
#pragma warning disable CA1031 // Aşama hatası cevapla bildirilir; ajan düşmez.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            Logger.Error(ex, "Üretim aşaması beklenmeyen hatayla durdu: {Stage}", command.Stage);
            return new AgentStageReply(false, AgentStageReply.Failed);
        }
        finally
        {
            _running.TryRemove(new KeyValuePair<Guid, CancellationTokenSource>(command.JobId, cancel));
        }
    }

    /// <inheritdoc />
    public void Cancel(Guid jobId)
    {
        if (_running.TryGetValue(jobId, out var cancel))
        {
            cancel.Cancel();
        }
    }

    /// <summary>Araç ya da model kurulu mu (0009 kurulum durumu).</summary>
    /// <param name="toolId">Araç kimliği.</param>
    /// <returns>Kuruluysa <c>true</c>.</returns>
    protected bool IsInstalled(string toolId) => setup.Load().Installed.ContainsKey(toolId);

    /// <summary>Görseller.</summary>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">Durdur belirteci.</param>
    /// <returns>Cevap.</returns>
    protected abstract Task<AgentStageReply> ImagesAsync(AgentStageCommand command, CancellationToken cancellationToken);

    /// <summary>Seslendirme.</summary>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">Durdur belirteci.</param>
    /// <returns>Cevap.</returns>
    protected abstract Task<AgentStageReply> VoiceAsync(AgentStageCommand command, CancellationToken cancellationToken);

    /// <summary>Render.</summary>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">Durdur belirteci.</param>
    /// <returns>Cevap.</returns>
    protected abstract Task<AgentStageReply> RenderAsync(AgentStageCommand command, CancellationToken cancellationToken);

    /// <summary>Birleştirme.</summary>
    /// <param name="command">Komut.</param>
    /// <param name="cancellationToken">Durdur belirteci.</param>
    /// <returns>Cevap.</returns>
    protected abstract Task<AgentStageReply> MergeAsync(AgentStageCommand command, CancellationToken cancellationToken);
}
