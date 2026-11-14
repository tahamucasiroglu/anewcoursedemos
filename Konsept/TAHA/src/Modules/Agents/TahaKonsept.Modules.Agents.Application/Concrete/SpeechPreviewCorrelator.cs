using System.Collections.Concurrent;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Bekleyen ses önizleme isteklerini bellekte tutan eşleyici (singleton).
/// </summary>
public sealed class SpeechPreviewCorrelator : SpeechPreviewCorrelatorBase
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<SpeechPreviewResult>> _pending = new();

    /// <summary>Bekleyen istek sayısı.</summary>
    public override int PendingCount => _pending.Count;

    /// <summary>Saklar.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <param name="source">Kaynak.</param>
    /// <returns>Eklendiyse <c>true</c>.</returns>
    protected override bool TryStore(Guid requestId, TaskCompletionSource<SpeechPreviewResult> source) => _pending.TryAdd(requestId, source);

    /// <summary>Çıkarır.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <param name="source">Kaynak.</param>
    /// <returns>Bulunduysa <c>true</c>.</returns>
    protected override bool TryTake(Guid requestId, out TaskCompletionSource<SpeechPreviewResult> source) =>
        _pending.TryRemove(requestId, out source!);
}
