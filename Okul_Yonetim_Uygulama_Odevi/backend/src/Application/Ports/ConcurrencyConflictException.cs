namespace OkulYonetim.Application.Ports;

/// <summary>Kaydedilmek istenen bir satırı başka bir istek önce değiştirdi (iyimser eşzamanlılık çakışması).</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
