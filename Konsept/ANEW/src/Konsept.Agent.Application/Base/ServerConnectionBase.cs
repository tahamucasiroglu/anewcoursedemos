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

    /// <inheritdoc />
    public event EventHandler<ConnectionState>? StateChanged;

    /// <inheritdoc />
    public event EventHandler? SessionRevoked;

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

    /// <summary>Kaynakları bırakır.</summary>
    /// <returns>Görev.</returns>
    protected abstract ValueTask DisposeCoreAsync();
}
