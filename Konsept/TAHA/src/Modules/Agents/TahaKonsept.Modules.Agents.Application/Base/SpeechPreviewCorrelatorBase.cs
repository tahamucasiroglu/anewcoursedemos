using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Base;

/// <summary>
/// Ses önizleme eşleyici tabanı: bekleyen istekleri <see cref="TaskCompletionSource{TResult}"/> ile tutar
/// (devamlar eşzamansız çalışır; hub iş parçacığını bloklamaz). Saklama türetilen sınıftadır.
/// </summary>
public abstract class SpeechPreviewCorrelatorBase : ISpeechPreviewCorrelator
{
    /// <summary>Bekleyen istek sayısı.</summary>
    public abstract int PendingCount { get; }

    /// <summary>Bekleyen istek kaydeder.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <returns>Yanıt görevi.</returns>
    public Task<SpeechPreviewResult> Register(Guid requestId)
    {
        var source = new TaskCompletionSource<SpeechPreviewResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!TryStore(requestId, source))
        {
            throw new InvalidOperationException($"Önizleme isteği zaten bekliyor: {requestId}.");
        }

        return source.Task;
    }

    /// <summary>Yanıtı iletir.</summary>
    /// <param name="result">Yanıt.</param>
    /// <returns>Bekleyen istek varsa <c>true</c>.</returns>
    public bool Complete(SpeechPreviewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return TryTake(result.RequestId, out var source) && source.TrySetResult(result);
    }

    /// <summary>İsteği iptal eder.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    public void Cancel(Guid requestId)
    {
        if (TryTake(requestId, out var source))
        {
            source.TrySetCanceled();
        }
    }

    /// <summary>Bekleyen isteği saklar.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <param name="source">Tamamlama kaynağı.</param>
    /// <returns>Eklenebildiyse <c>true</c>.</returns>
    protected abstract bool TryStore(Guid requestId, TaskCompletionSource<SpeechPreviewResult> source);

    /// <summary>Bekleyen isteği çıkarır.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <param name="source">Tamamlama kaynağı.</param>
    /// <returns>Bulunduysa <c>true</c>.</returns>
    protected abstract bool TryTake(Guid requestId, out TaskCompletionSource<SpeechPreviewResult> source);
}
