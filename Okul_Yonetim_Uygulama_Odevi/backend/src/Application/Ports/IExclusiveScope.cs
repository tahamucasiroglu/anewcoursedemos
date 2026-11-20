namespace OkulYonetim.Application.Ports;

/// <summary>
/// Bir anahtar (ör. e-posta) için tek seferde tek isteğin çalıştığı işlem: açılınca aynı anahtarlı diğer istekler bekler.
/// <see cref="CommitAsync"/> çağrılmadan kapatılırsa yazılanlar geri alınır. Kilit işlem bitince kendiliğinden bırakılır.
/// </summary>
public interface IExclusiveScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
