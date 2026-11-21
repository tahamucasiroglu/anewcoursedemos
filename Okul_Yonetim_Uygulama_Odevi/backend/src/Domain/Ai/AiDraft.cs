namespace OkulYonetim.Domain.Ai;

/// <summary>
/// AI'ın ürettiği, insan onayı bekleyen çıktı (<c>docs/domain.md</c>, BR-5, BR-6; spec 0012): okul, tür, oluşturan kişi,
/// öğrenci, isteğe bağlı bağlam (sesli notta konuşma), kaynak metin (sesli notun dökümü — BR-6), taslak metin
/// (en fazla <see cref="DraftMaxLength"/>), durum ve onay bilgisi (son metin, onaylayan, zaman, sonuç kaydı — gönderilen mesaj).
/// Taslak bir kez kullanılır: <see cref="Approve"/> yalnızca "bekliyor"dan; ikinci kez <see cref="InvalidOperationException"/>
/// (çağıran önce <see cref="IsPending"/> ile denetler ve kullanıcıya "Bu taslak zaten kullanıldı." der). Ses saklanmaz.
/// <para>
/// Spec 0014 — haftalık gelişim özeti (<see cref="WeeklySummary"/>): bağlam yok, dönem <see cref="PeriodStart"/> (haftanın
/// Pazartesi'si; öğrenci-hafta başına tek taslak — veritabanında tekil dizin), kaynak = dayanak (yoklama, ödev/not, öğretmen
/// notu; JSON), taslak en fazla <see cref="SummaryMaxLength"/>. Bekleyen özet yeniden oluşturulunca aynı kayıt güncellenir
/// (<see cref="Regenerate"/>); onaylanmış özet yeniden oluşturulamaz. Özeti sınıfın atanmış her öğretmeni onaylayabilir
/// (onaylayan ayrı yazılır); sonuç kaydı yoktur (veli başına bildirimler kaynak olarak taslağı gösterir).
/// </para>
/// </summary>
public sealed class AiDraft
{
    public const int DraftMaxLength = 1000;
    public const int SummaryMaxLength = 800;

    private AiDraft(
        Guid id,
        Guid tenantId,
        AiDraftKind kind,
        Guid createdByUserId,
        Guid studentId,
        Guid? contextId,
        string sourceText,
        string draftText,
        DateTimeOffset createdAt,
        DateOnly? periodStart = null)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.Kind = kind;
        this.CreatedByUserId = createdByUserId;
        this.StudentId = studentId;
        this.ContextId = contextId;
        this.SourceText = sourceText;
        this.DraftText = draftText;
        this.Status = AiDraftStatus.Pending;
        this.CreatedAt = createdAt;
        this.PeriodStart = periodStart;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public AiDraftKind Kind { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid? ContextId { get; private set; }

    public string SourceText { get; private set; }

    public string DraftText { get; private set; }

    public string? FinalText { get; private set; }

    public AiDraftStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public Guid? ResultId { get; private set; }

    public DateOnly? PeriodStart { get; private set; }

    public bool IsPending => this.Status == AiDraftStatus.Pending;

    /// <summary>Sesli nottan veli mesajı taslağı: bağlam konuşmadır; taslak <see cref="DraftMaxLength"/>'e kısaltılır.</summary>
    public static AiDraft VoiceMessage(Guid tenantId, Guid teacherUserId, Guid studentId, Guid conversationId, string transcript, string draft, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(draft);
        return new(Guid.CreateVersion7(now), tenantId, AiDraftKind.VoiceMessage, teacherUserId, studentId, conversationId, transcript.Trim(), Shorten(draft), now);
    }

    /// <summary>Haftalık gelişim özeti taslağı (spec 0014): dönem haftanın Pazartesi'si, kaynak dayanak; taslak <see cref="SummaryMaxLength"/>'e kısaltılır.</summary>
    public static AiDraft WeeklySummary(Guid tenantId, Guid teacherUserId, Guid studentId, DateOnly weekStart, string source, string draft, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(draft);
        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentOutOfRangeException(nameof(weekStart), "Hafta Pazartesi başlar.");
        }

        return new(Guid.CreateVersion7(now), tenantId, AiDraftKind.WeeklySummary, teacherUserId, studentId, null, source, Shorten(draft, SummaryMaxLength), now, weekStart);
    }

    /// <summary>Taslağı en fazla <see cref="DraftMaxLength"/> karaktere kısaltır (baştaki/sondaki boşluk atılır).</summary>
    public static string Shorten(string text) => Shorten(text, DraftMaxLength);

    /// <summary>Taslağı en fazla <paramref name="max"/> karaktere kısaltır (baştaki/sondaki boşluk atılır).</summary>
    public static string Shorten(string text, int max)
    {
        ArgumentNullException.ThrowIfNull(text);
        string trimmed = text.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max].TrimEnd();
    }

    /// <summary>
    /// Bekleyen haftalık özeti yeniden oluşturur (spec 0014 gereksinim 3): aynı kayıt yeni kaynak, taslak, oluşturan ve zamanla
    /// güncellenir — öğrenci-hafta başına tek bekleyen taslak kalır. Onaylanmış taslakta <see cref="InvalidOperationException"/>.
    /// </summary>
    public void Regenerate(Guid teacherUserId, string source, string draft, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(draft);
        if (!this.IsPending)
        {
            throw new InvalidOperationException("Onaylanmış AI taslağı yeniden oluşturulamaz.");
        }

        if (this.Kind != AiDraftKind.WeeklySummary)
        {
            throw new InvalidOperationException("Yalnızca haftalık özet yeniden oluşturulur.");
        }

        this.CreatedByUserId = teacherUserId;
        this.SourceText = source;
        this.DraftText = Shorten(draft, SummaryMaxLength);
        this.CreatedAt = now;
    }

    /// <summary>İnsan onayı: son metin, onaylayan, zaman ve sonuç kaydı (ör. gönderilen mesaj; haftalık özette yok) yazılır; yalnızca bir kez.</summary>
    public void Approve(string finalText, Guid approvedByUserId, Guid? resultId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(finalText);
        if (!this.IsPending)
        {
            throw new InvalidOperationException("AI taslağı zaten kullanıldı.");
        }

        this.FinalText = finalText;
        this.ApprovedByUserId = approvedByUserId;
        this.ApprovedAt = now;
        this.ResultId = resultId;
        this.Status = AiDraftStatus.Approved;
    }
}
