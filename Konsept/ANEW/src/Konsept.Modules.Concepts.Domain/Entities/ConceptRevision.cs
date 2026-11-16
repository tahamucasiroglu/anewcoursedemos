using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Talimatla yeniden üretilen konsept revizyonu (spec 0015 Req 7–12). Api süreci belleğinde tutulur (kalıcı değildir); yürütücü sonucu
/// yazar, tarayıcı yoklayarak okur; tüm değişiklikler kilit altındadır. Revizyon onaylanana ya da silinene kadar konsept değişmez.
/// </summary>
public sealed class ConceptRevision
{
    private readonly Lock _gate = new();
    private RevisionStatus _status = RevisionStatus.Running;
    private IReadOnlyList<ConceptScene> _scenes = [];
    private bool _wordRepetition;
    private bool _durationMismatch;
    private string? _error;

    /// <summary>Gets kimlik (her tur yeni revizyon).</summary>
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets konsept.</summary>
    public Guid ConceptId { get; init; }

    /// <summary>Gets a value indicating whether yeniden üretim sürüyor.</summary>
    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _status == RevisionStatus.Running;
            }
        }
    }

    /// <summary>Revizyonu hazır yapar.</summary>
    /// <param name="scenes">Sahneler.</param>
    /// <param name="wordRepetition">Kelime tekrarı uyarısı.</param>
    /// <param name="durationMismatch">Süre uyumsuz uyarısı.</param>
    public void Complete(IReadOnlyList<ConceptScene> scenes, bool wordRepetition, bool durationMismatch)
    {
        lock (_gate)
        {
            _scenes = scenes;
            _wordRepetition = wordRepetition;
            _durationMismatch = durationMismatch;
            _status = RevisionStatus.Ready;
        }
    }

    /// <summary>Yeniden üretim başarısız (revizyon yok).</summary>
    /// <param name="message">Sade hata mesajı.</param>
    public void Fail(string message)
    {
        lock (_gate)
        {
            _error = message;
            _status = RevisionStatus.Failed;
        }
    }

    /// <summary>Tutarlı anlık görüntü.</summary>
    /// <returns>Görüntü.</returns>
    public ConceptRevisionSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new ConceptRevisionSnapshot(Id, ConceptId, _status, _scenes, _wordRepetition, _durationMismatch, _error);
        }
    }
}
