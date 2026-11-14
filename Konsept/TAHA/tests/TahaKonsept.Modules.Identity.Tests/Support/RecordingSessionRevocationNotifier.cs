using System.Collections.Concurrent;
using TahaKonsept.Modules.Identity.Application.Base;

namespace TahaKonsept.Modules.Identity.Tests.Support;

/// <summary>
/// Oturum iptal bildirimlerini kaydeden test bildirimcisi (Agents modülünün yapacağını taklit eder).
/// </summary>
public sealed class RecordingSessionRevocationNotifier : SessionRevocationNotifierBase
{
    /// <summary>"Tüm oturumlar iptal" bildirilen kullanıcılar.</summary>
    public ConcurrentQueue<Guid> AllRevoked { get; } = new();

    /// <summary>"Aile iptal" bildirimleri (kullanıcı, aile).</summary>
    public ConcurrentQueue<(Guid UserId, Guid FamilyId)> FamiliesRevoked { get; } = new();

    /// <inheritdoc />
    public override Task OnAllSessionsRevokedAsync(Guid userId, CancellationToken ct = default)
    {
        AllRevoked.Enqueue(userId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task OnTokenFamilyRevokedAsync(Guid userId, Guid familyId, CancellationToken ct = default)
    {
        FamiliesRevoked.Enqueue((userId, familyId));
        return Task.CompletedTask;
    }
}
