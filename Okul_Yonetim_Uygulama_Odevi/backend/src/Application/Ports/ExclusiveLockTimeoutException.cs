namespace OkulYonetim.Application.Ports;

/// <summary>
/// Sıraya sokulmuş işlem (<see cref="IUnitOfWork.BeginExclusiveAsync"/>) kendi sırasını üst sınır içinde alamadı: aynı
/// anahtara çok sayıda istek yığılmış. Bekleyen istek bağlantıyı süresiz tutmaz (spec 0004 review I-4).
/// </summary>
public sealed class ExclusiveLockTimeoutException : Exception
{
    public ExclusiveLockTimeoutException()
    {
    }

    public ExclusiveLockTimeoutException(string message)
        : base(message)
    {
    }

    public ExclusiveLockTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
