using System.Collections.Concurrent;
using TahaKonsept.Modules.Agents.Application.Base;
using TahaKonsept.Shared.Contracts.Agent;

namespace TahaKonsept.Modules.Agents.Application.Concrete;

/// <summary>
/// Bekleyen ses önizleme isteklerini (sahip kullanıcıyla birlikte) bellekte tutan eşleyici (singleton).
/// </summary>
public sealed class SpeechPreviewCorrelator : SpeechPreviewCorrelatorBase
{
    private readonly ConcurrentDictionary<Guid, (Guid Owner, TaskCompletionSource<SpeechPreviewResult> Source)> _pending = new();

    /// <summary>Bekleyen istek sayısı.</summary>
    public override int PendingCount => _pending.Count;

    /// <summary>Saklar.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <param name="ownerId">Sahip kullanıcı.</param>
    /// <param name="source">Kaynak.</param>
    /// <returns>Eklendiyse <c>true</c>.</returns>
    protected override bool TryStore(Guid requestId, Guid ownerId, TaskCompletionSource<SpeechPreviewResult> source) =>
        _pending.TryAdd(requestId, (ownerId, source));

    /// <summary>Sahibi okur.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <param name="ownerId">Sahip kullanıcı.</param>
    /// <returns>Bulunduysa <c>true</c>.</returns>
    protected override bool TryGetOwner(Guid requestId, out Guid ownerId)
    {
        var found = _pending.TryGetValue(requestId, out var entry);
        ownerId = entry.Owner;
        return found;
    }

    /// <summary>Çıkarır.</summary>
    /// <param name="requestId">İstek kimliği.</param>
    /// <param name="source">Kaynak.</param>
    /// <returns>Bulunduysa <c>true</c>.</returns>
    protected override bool TryTake(Guid requestId, out TaskCompletionSource<SpeechPreviewResult> source)
    {
        var found = _pending.TryRemove(requestId, out var entry);
        source = entry.Source;
        return found;
    }
}
