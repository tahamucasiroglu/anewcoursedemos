using Konsept.Modules.Monitoring.Application.Abstract;

namespace Konsept.Modules.Monitoring.Infrastructure.Persistence.Base;

/// <summary>
/// Veritabanı yoklamalarının temeli: bağlantı hatası durum bilgisine dönüşür, exception yükselmez.
/// </summary>
public abstract class DatabaseProbeBase : IDatabaseProbe
{
    /// <inheritdoc />
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ProbeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>Asıl yoklamayı yapar.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Erişilebilirse <c>true</c>.</returns>
    protected abstract Task<bool> ProbeAsync(CancellationToken cancellationToken);
}
