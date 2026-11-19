namespace OkulYonetim.Application.Ports;

/// <summary>
/// Veritabanına ulaşılabiliyor mu sorusunu soran port. Uygulaması Persistence katmanındadır;
/// <c>CanConnectAsync</c> bağlantı kurulabiliyorsa true, kurulamıyorsa false döner ve iç hatayı dışarı sızdırmaz.
/// </summary>
public interface IDatabaseHealthProbe
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
