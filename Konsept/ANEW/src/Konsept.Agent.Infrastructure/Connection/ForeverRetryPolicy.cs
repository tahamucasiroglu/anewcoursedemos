using System.Net;
using Microsoft.AspNetCore.SignalR.Client;

namespace Konsept.Agent.Infrastructure.Connection;

/// <summary>
/// Kopan bağlantıyı vazgeçmeden, sabit aralıkla yeniden dener (Pi yeniden başlasa da ajan geri bağlanır). Sunucu ajan
/// oturumunu reddederse (401; çıkış, yeni eşleştirme ya da 90 gün) eski oturumla denemeyi bırakır (spec 0012 Req 6, 12).
/// </summary>
public sealed class ForeverRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan _delay;

    /// <summary>Initializes a new instance of the <see cref="ForeverRetryPolicy"/> class.</summary>
    /// <param name="delay">Denemeler arası bekleme.</param>
    public ForeverRetryPolicy(TimeSpan delay)
    {
        _delay = delay;
    }

    /// <inheritdoc />
    public TimeSpan? NextRetryDelay(RetryContext retryContext) =>
        retryContext?.RetryReason is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } ? null
        : retryContext?.PreviousRetryCount == 0 ? TimeSpan.Zero
        : _delay;
}
