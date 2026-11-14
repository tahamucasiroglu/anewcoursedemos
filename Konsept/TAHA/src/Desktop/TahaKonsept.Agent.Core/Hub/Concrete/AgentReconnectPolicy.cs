using Microsoft.AspNetCore.SignalR.Client;

namespace TahaKonsept.Agent.Core.Hub.Concrete;

/// <summary>
/// Sonsuz yeniden bağlanma politikası: 0, 2, 5, 10, 20 sn, sonra her 30 sn. Her denemede <c>onRetry</c> çağrılır
/// (ör. 401 nedeniyle koptuysa token yenilemek için).
/// </summary>
public sealed class AgentReconnectPolicy : IRetryPolicy
{
    private static readonly TimeSpan[] Delays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(20),
    ];

    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    private readonly Action<Exception?>? _onRetry;

    /// <summary>Oluşturur.</summary>
    /// <param name="onRetry">Her denemede çağrılacak geri çağrı (koparan hata ile).</param>
    public AgentReconnectPolicy(Action<Exception?>? onRetry = null)
    {
        _onRetry = onRetry;
    }

    /// <summary>Deneme sayısına göre bekleme süresi.</summary>
    /// <param name="previousRetryCount">Önceki deneme sayısı.</param>
    /// <returns>Bekleme süresi.</returns>
    public static TimeSpan GetDelay(long previousRetryCount) =>
        previousRetryCount < Delays.Length ? Delays[previousRetryCount] : MaxDelay;

    /// <inheritdoc />
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        ArgumentNullException.ThrowIfNull(retryContext);
        _onRetry?.Invoke(retryContext.RetryReason);
        return GetDelay(retryContext.PreviousRetryCount);
    }
}
