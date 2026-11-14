namespace Konsept.Modules.Monitoring.Application.Abstract;

/// <summary>
/// Veritabanının erişilebilir olup olmadığını yoklar.
/// </summary>
public interface IDatabaseProbe
{
    /// <summary>Veritabanına bağlanmayı dener.</summary>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Erişilebiliyorsa <c>true</c>; hata fırlatmaz.</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
