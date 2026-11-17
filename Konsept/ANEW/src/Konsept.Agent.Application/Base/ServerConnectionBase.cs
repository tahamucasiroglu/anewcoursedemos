using Konsept.Agent.Application.Abstract;
using Konsept.Agent.Domain.Entities;
using Konsept.Agent.Domain.Enums;

namespace Konsept.Agent.Application.Base;

/// <summary>
/// Sunucu bağlantılarının ortak durum yönetimi: durum değişince olayı yalnızca bir kez tetikler.
/// </summary>
public abstract class ServerConnectionBase : IServerConnection
{
    private int _state = (int)ConnectionState.Disconnected;
    private volatile bool _updateRequired;

    /// <inheritdoc />
    public event EventHandler<ConnectionState>? StateChanged;

    /// <inheritdoc />
    public event EventHandler? SessionRevoked;

    /// <inheritdoc />
    public event EventHandler<string>? UpdateRequiredReceived;

    /// <inheritdoc />
    public bool UpdateRequired => _updateRequired;

    /// <inheritdoc />
    public ConnectionState State => (ConnectionState)Volatile.Read(ref _state);

    /// <inheritdoc />
    public abstract Task StartAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task StopAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task SuspendAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task<bool> TryReportLogAsync(LogSummary summary, CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task<Guid?> TryGetAccountAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task<ReportOutcome?> TrySendErrorReportAsync(AgentErrorReport report, CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task<bool?> TryBeginUpdateAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public abstract Task TryEndUpdateAsync();

    /// <inheritdoc />
    public abstract Task<bool> TryReportComponentsAsync(CancellationToken cancellationToken);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>Durumu günceller ve değiştiyse olayı tetikler.</summary>
    /// <param name="state">Yeni durum.</param>
    protected void SetState(ConnectionState state)
    {
        if (Interlocked.Exchange(ref _state, (int)state) != (int)state)
        {
            StateChanged?.Invoke(this, state);
        }
    }

    /// <summary>Sunucunun oturumu kapattığını bildirir.</summary>
    protected void RaiseSessionRevoked() => SessionRevoked?.Invoke(this, EventArgs.Empty);

    /// <summary>Kayıt cevabındaki sürüm bilgisini işler (spec 0023 Req 4): uyumsuzsa komutlar durur ve olay yükselir.</summary>
    /// <param name="updateRequired">Sürüm uyumsuz mu.</param>
    /// <param name="serverVersion">Sunucu sürümü.</param>
    protected void SetUpdateRequired(bool updateRequired, string serverVersion)
    {
        _updateRequired = updateRequired;
        if (updateRequired)
        {
            UpdateRequiredReceived?.Invoke(this, serverVersion);
        }
    }

    /// <summary>Kaynakları bırakır.</summary>
    /// <returns>Görev.</returns>
    protected abstract ValueTask DisposeCoreAsync();
}
