using Konsept.Modules.Monitoring.Application.Abstract;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Veritabanının erişilebilirliğini test içinden ayarlamaya yarar.
/// </summary>
public sealed class FakeDatabaseProbe : IDatabaseProbe
{
    /// <summary>Gets or sets a value indicating whether veritabanı erişilebilir görünür.</summary>
    public bool Available { get; set; } = true;

    /// <inheritdoc />
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) => Task.FromResult(Available);
}
