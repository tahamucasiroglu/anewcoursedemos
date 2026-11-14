using TahaKonsept.Agent.Engines.Speech.Python.Protocol;
using TahaKonsept.BuildingBlocks.Domain.Results;

namespace TahaKonsept.Agent.Engines.Speech.Python.Abstract;

/// <summary>
/// Uzun ömürlü <c>tts_worker.py</c> işçisiyle konuşan istemci: bir JSON satırı yazar, tek yanıt satırını bekler.
/// İstekler sıraya alınır (aynı anda tek istek). <see cref="IsRunning"/> işçi süreci yaşıyor mu.
/// </summary>
public interface ITtsWorkerClient : IAsyncDisposable
{
    /// <summary>İşçi çalışıyor mu.</summary>
    bool IsRunning { get; }

    /// <summary>İsteği gönderir ve yanıtı bekler. <c>ok=false</c> yanıtı da başarılı bir <see cref="Result{T}"/> olarak döner;
    /// hata yalnızca iletişim sorununda (süreç öldü, zaman aşımı, protokol) döner.</summary>
    /// <param name="request">İstek.</param>
    /// <param name="timeout">Yanıt için en uzun süre.</param>
    /// <param name="ct">İptal belirteci.</param>
    /// <returns>Yanıt ya da iletişim hatası.</returns>
    Task<Result<WorkerResponse>> SendAsync(WorkerRequest request, TimeSpan timeout, CancellationToken ct);
}
