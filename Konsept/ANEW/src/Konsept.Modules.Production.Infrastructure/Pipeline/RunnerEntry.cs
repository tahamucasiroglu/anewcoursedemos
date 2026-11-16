using Konsept.Modules.AiGateway.Contracts.Dtos;

namespace Konsept.Modules.Production.Infrastructure.Pipeline;

/// <summary>
/// Bir kullanıcının koşucu kaydı (spec 0017): çağıran kimliği, Durdur belirteci ve döngü bayrakları (çalışıyor / yeniden değerlendirme
/// bekliyor). Bayraklar kilitsiz atomik işlemlerle değişir.
/// </summary>
/// <param name="caller">İlk kimlik.</param>
internal sealed class RunnerEntry(ModelCallerIdentity caller)
{
    private int _running;
    private int _pending;

    /// <summary>Gets or sets çağıran kimliği.</summary>
    public ModelCallerIdentity Caller { get; set; } = caller;

    /// <summary>Gets or sets Durdur belirteci kaynağı.</summary>
    public CancellationTokenSource Stop { get; set; } = new();

    /// <summary>Gets a value indicating whether döngü çalışıyor.</summary>
    public bool Running => Volatile.Read(ref _running) == 1;

    /// <summary>Yeniden değerlendirme ister.</summary>
    public void MarkPending() => Interlocked.Exchange(ref _pending, 1);

    /// <summary>Bekleyen değerlendirmeyi alır.</summary>
    /// <returns>Bekleyen vardıysa <c>true</c>.</returns>
    public bool TakePending() => Interlocked.Exchange(ref _pending, 0) == 1;

    /// <summary>Bekleyen değerlendirme var mı.</summary>
    /// <returns>Varsa <c>true</c>.</returns>
    public bool HasPending() => Volatile.Read(ref _pending) == 1;

    /// <summary>Döngüyü başlatma hakkını alır.</summary>
    /// <returns>Alındıysa <c>true</c>.</returns>
    public bool TryStart() => Interlocked.CompareExchange(ref _running, 1, 0) == 0;

    /// <summary>Döngü bitti.</summary>
    public void Finish() => Volatile.Write(ref _running, 0);
}
