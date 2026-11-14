using System.Collections.Concurrent;

namespace Konsept.Api.Tests.Fakes;

/// <summary>
/// Kayıt isteklerinin ilk birkaçını reddettirmek için paylaşılan sayaç; görülen bağlantı kimliklerini tutar.
/// </summary>
/// <param name="failuresBeforeSuccess">Kaç kayıt isteği reddedilsin.</param>
public sealed class FlakyRegistrationState(int failuresBeforeSuccess)
{
    private int _attempts;

    /// <summary>Gets kayıt isteklerinin geldiği bağlantı kimlikleri.</summary>
    public ConcurrentBag<string> ConnectionIds { get; } = [];

    /// <summary>Gets toplam kayıt isteği sayısı.</summary>
    public int Attempts => Volatile.Read(ref _attempts);

    /// <summary>Yeni bir kayıt isteğini sayar.</summary>
    /// <param name="connectionId">Bağlantı kimliği.</param>
    /// <returns>Bu istek reddedilecekse <c>true</c>.</returns>
    public bool ShouldFail(string connectionId)
    {
        ConnectionIds.Add(connectionId);
        return Interlocked.Increment(ref _attempts) <= failuresBeforeSuccess;
    }
}
