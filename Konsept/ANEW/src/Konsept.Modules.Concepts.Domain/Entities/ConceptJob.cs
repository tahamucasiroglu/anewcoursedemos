using Konsept.Modules.Concepts.Domain.Enums;

namespace Konsept.Modules.Concepts.Domain.Entities;

/// <summary>
/// Sunucuda süren bir üretim partisi (spec 0014 Req 17, 19–20). Yürütücü ilerlemeyi yazar, tarayıcı yoklayarak okur; tüm değişiklikler
/// kilit altındadır. <see cref="RequestStop"/> yalnızca bayrak koyar: süren çağrı tamamlanır, sıradakiler başlamaz (G-3).
/// </summary>
public sealed class ConceptJob
{
    private readonly Lock _gate = new();
    private readonly List<JobFailure> _failures = [];
    private JobStatus _status = JobStatus.Running;
    private int _processed;
    private int _succeeded;
    private int _formatRejected;
    private int _duplicates;
    private bool _dedupeUnavailable;
    private bool _stopRequested;
    private string? _message;

    /// <summary>Gets kimlik.</summary>
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Gets sahibi.</summary>
    public Guid UserId { get; init; }

    /// <summary>Gets profil.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Gets profil adı (Günlük etiketi).</summary>
    public required string ProfileName { get; init; }

    /// <summary>Gets şerit.</summary>
    public JobLane Lane { get; init; }

    /// <summary>Gets tür.</summary>
    public JobKind Kind { get; init; }

    /// <summary>Gets hedef adet.</summary>
    public int Total { get; init; }

    /// <summary>Gets başlangıç (UTC).</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>Gets a value indicating whether durdurma istendi.</summary>
    public bool StopRequested
    {
        get
        {
            lock (_gate)
            {
                return _stopRequested;
            }
        }
    }

    /// <summary>Gets a value indicating whether iş sürüyor.</summary>
    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _status == JobStatus.Running;
            }
        }
    }

    /// <summary>Durdurma ister (sürüyorsa).</summary>
    /// <returns>Sürüyorduysa <c>true</c>.</returns>
    public bool RequestStop()
    {
        lock (_gate)
        {
            if (_status != JobStatus.Running)
            {
                return false;
            }

            _stopRequested = true;
            return true;
        }
    }

    /// <summary>Başarılı adım (konsept kaydedildi).</summary>
    public void Succeed()
    {
        lock (_gate)
        {
            _processed++;
            _succeeded++;
        }
    }

    /// <summary>İşlendi ama sonuç yerinde bekliyor (fikirden üretimde parti sonu denetimi).</summary>
    public void Advance()
    {
        lock (_gate)
        {
            _processed++;
        }
    }

    /// <summary>Kaydedilenleri sayar (işlenen sayıyı değiştirmez).</summary>
    /// <param name="count">Adet.</param>
    public void AddSaved(int count)
    {
        lock (_gate)
        {
            _succeeded += count;
        }
    }

    /// <summary>Üretilemeyen adım.</summary>
    /// <param name="label">Etiket.</param>
    /// <param name="message">Sade mesaj.</param>
    public void Fail(string label, string message)
    {
        lock (_gate)
        {
            _processed++;
            _failures.Add(new JobFailure(label, message));
        }
    }

    /// <summary>Öneri sonuçlarını yazar.</summary>
    /// <param name="listed">Listelenen.</param>
    /// <param name="formatRejected">Biçim dışı.</param>
    /// <param name="duplicates">Tekrar.</param>
    /// <param name="dedupeUnavailable">Anlamca denetim yapılamadı.</param>
    public void SetTopicResult(int listed, int formatRejected, int duplicates, bool dedupeUnavailable)
    {
        lock (_gate)
        {
            _processed = Total;
            _succeeded = listed;
            _formatRejected = formatRejected;
            _duplicates = duplicates;
            _dedupeUnavailable = dedupeUnavailable;
        }
    }

    /// <summary>Kaydedilmeyen tekrarları ve denetim durumunu ekler.</summary>
    /// <param name="duplicates">Tekrar adedi.</param>
    /// <param name="dedupeUnavailable">Anlamca denetim yapılamadı.</param>
    public void AddDuplicates(int duplicates, bool dedupeUnavailable)
    {
        lock (_gate)
        {
            _duplicates += duplicates;
            _dedupeUnavailable |= dedupeUnavailable;
        }
    }

    /// <summary>İşi bitirir: durdurulduysa "durduruldu", hiçbir şey üretilemediyse "başarısız", aksi halde "bitti".</summary>
    /// <param name="failureMessage">Tamamen başarısız işin mesajı (varsa).</param>
    /// <returns>Son durum.</returns>
    public JobStatus Finish(string? failureMessage = null)
    {
        lock (_gate)
        {
            if (failureMessage is not null)
            {
                _message = failureMessage;
                _status = JobStatus.Failed;
            }
            else if (_stopRequested)
            {
                _status = JobStatus.Stopped;
            }
            else if (Kind != JobKind.Topics && _succeeded == 0 && _duplicates == 0 && _failures.Count > 0)
            {
                _message = _failures[^1].Message;
                _status = JobStatus.Failed;
            }
            else
            {
                _status = JobStatus.Completed;
            }

            return _status;
        }
    }

    /// <summary>Anlık görüntü.</summary>
    /// <returns>Görüntü.</returns>
    public ConceptJobSnapshot Snapshot()
    {
        lock (_gate)
        {
            return new ConceptJobSnapshot(
                Id, ProfileId, Lane, Kind, _status, Total, _processed, _succeeded, _formatRejected, _duplicates, _dedupeUnavailable, [.. _failures], _message);
        }
    }
}
