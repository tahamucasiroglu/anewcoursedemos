using Konsept.Shared.Logging.Abstract;

namespace Konsept.Shared.Logging.Tests;

/// <summary>Kaç kez çağrıldığını sayan, istenirse istisna fırlatan sahte temizleyici.</summary>
/// <param name="failure">Her çağrıda fırlatılacak istisna; <c>null</c> ise fırlatmaz.</param>
public sealed class CountingJanitor(Exception? failure = null) : ILogFileJanitor
{
    private int _calls;

    /// <summary>Gets çağrı sayısı.</summary>
    public int Calls => Volatile.Read(ref _calls);

    /// <inheritdoc />
    public int Clean(string directory)
    {
        Interlocked.Increment(ref _calls);
        return failure is null ? 0 : throw failure;
    }
}
