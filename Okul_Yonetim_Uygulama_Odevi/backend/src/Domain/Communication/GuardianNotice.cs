namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Veliye giden uygulama içi bildirim (spec 0010 gereksinim 6–8): kurum, veli (kişi), öğrenci, gün ve tür. Veli + öğrenci +
/// gün tekildir — aynı gün ikinci "yok" bildirimi oluşmaz (C-5). "Yok" düzeltilince aynı satır <see cref="GuardianNoticeKind.Corrected"/>
/// olur ("düzeltildi" etiketi, düzeltme metni) ve yeniden okunmamış sayılır; yeniden "Yok" olursa tekrar devamsızlık
/// bildirimi olur. Metin saklanmaz: ekranda tür, öğrenci adı, gün ve okul adından kurulur (gizlilik, gereksinim 10).
/// <para>
/// Spec 0013: diğer modüllerin kayda bağlı okul bildirimi (<see cref="GuardianNoticeKind.Assignment"/> — yeni ödev) kaynak kaydın
/// kimliğini (<see cref="SourceId"/>) ve hazır metnini (<see cref="Text"/>: ders, başlık, teslim günü — öğrenci adı yok) taşır;
/// veli + öğrenci + kaynak tekildir. Yoklama bildiriminde ikisi de boştur ve veli + öğrenci + gün tekilliği yalnızca onlar içindir.
/// </para>
/// <para>
/// Spec 0014: haftalık gelişim özeti (<see cref="GuardianNoticeKind.WeeklySummary"/>) — kaynak onaylanan AI taslağı, gün haftanın
/// Pazartesi'si, metin öğretmenin onayladığı özet + "Yapay zekâ desteğiyle hazırlandı, öğretmen onayladı." + dayanak satırı (en fazla
/// <see cref="SummaryTextMax"/>; diğer kayda bağlı bildirimler <see cref="TextMax"/>).
/// </para>
/// <para>
/// Spec 0016: randevu bildirimi (<see cref="GuardianNoticeKind.Appointment"/>) — kaynak onaylanan/reddedilen randevu ya da öğretmenin
/// iptaliyle kapanan dilim; metin öğretmen adı, gün, saat ve varsa neden (en fazla <see cref="AppointmentTextMax"/>: ad 200 + neden 300
/// + kalıp); görüşme konusu yok.
/// </para>
/// </summary>
public sealed class GuardianNotice
{
    public const int TextMax = 400;
    public const int SummaryTextMax = 2000;
    public const int AppointmentTextMax = 600;

    private GuardianNotice(Guid id, Guid tenantId, Guid guardianUserId, Guid studentId, DateOnly day, GuardianNoticeKind kind, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.GuardianUserId = guardianUserId;
        this.StudentId = studentId;
        this.Day = day;
        this.Kind = kind;
        this.CreatedAt = createdAt;
        this.UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid GuardianUserId { get; private set; }

    public Guid StudentId { get; private set; }

    public DateOnly Day { get; private set; }

    public GuardianNoticeKind Kind { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? SourceId { get; private set; }

    public string? Text { get; private set; }

    public static GuardianNotice Absent(Guid tenantId, Guid guardianUserId, Guid studentId, DateOnly day, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, guardianUserId, studentId, day, GuardianNoticeKind.Absent, now, now);

    /// <summary>Kayda bağlı okul bildirimi (spec 0013: yeni ödev; spec 0014: haftalık özet); metin hazırdır.</summary>
    public static GuardianNotice ForRecord(Guid tenantId, Guid guardianUserId, Guid studentId, DateOnly day, GuardianNoticeKind kind, Guid sourceId, string text, DateTimeOffset now)
    {
        int max = kind switch
        {
            GuardianNoticeKind.WeeklySummary => SummaryTextMax,
            GuardianNoticeKind.Appointment => AppointmentTextMax,
            _ => TextMax,
        };
        if (kind is GuardianNoticeKind.Absent or GuardianNoticeKind.Corrected || string.IsNullOrWhiteSpace(text) || text.Length > max)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), "Kayda bağlı bildirim türü ve metni geçersiz.");
        }

        return new(Guid.CreateVersion7(now), tenantId, guardianUserId, studentId, day, kind, now, now) { SourceId = sourceId, Text = text };
    }

    /// <summary>Türü değiştirir ve bildirimi yeniden okunmamış yapar (düzeltme ya da yeniden devamsızlık).</summary>
    public void Change(GuardianNoticeKind kind, DateTimeOffset now)
    {
        this.Kind = kind;
        this.ReadAt = null;
        this.UpdatedAt = now;
    }

    public void MarkRead(DateTimeOffset now) => this.ReadAt ??= now;
}
