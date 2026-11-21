namespace OkulYonetim.Domain.Appointments;

/// <summary>
/// Velinin bir dilim için randevu talebi (spec 0016 C-3, C-4): okul, dilim, öğretmen, talep eden veli, çocuk, dilimin başlangıç ve
/// bitişi (dilimden kopyalanır — öğretmen iptaliyle kapanan dilimin randevusu da gün ve saatiyle görünür), konu (zorunlu, en fazla
/// <see cref="TopicMax"/>), durum, isteğe bağlı ret/iptal nedeni (en fazla <see cref="ReasonMax"/>) ve zamanlar.
/// <para>
/// Geçişler: onay bekleyen talep başlamadan onaylanır ya da reddedilir (<see cref="Approve"/>, <see cref="Reject"/>); veli onay
/// bekleyen ya da onaylı randevusunu başlamadan iptal eder (<see cref="CancelByGuardian"/>); öğretmen yalnızca onaylı randevuyu
/// başlamadan iptal eder (<see cref="CancelByTeacher"/>). Geçersiz geçiş <see cref="InvalidOperationException"/>; işleyici önce
/// <c>Can…</c> denetimleriyle Türkçe iş hatasını verir. "Süresi doldu" saklanmaz: <see cref="StateAt"/> başlangıç ≤ şimdi olan onay
/// bekleyen talebi <see cref="AppointmentState.Expired"/> gösterir. Açık randevu = onay bekleyen ya da onaylı ve başlamamış
/// (<see cref="IsOpenAt"/>).
/// </para>
/// </summary>
public sealed class Appointment
{
    public const int TopicMax = 300;
    public const int ReasonMax = 300;

    private Appointment(Guid id, AvailabilitySlot slot, Guid guardianUserId, Guid studentId, string topic, DateTimeOffset now)
    {
        this.Id = id;
        this.TenantId = slot.TenantId;
        this.SlotId = slot.Id;
        this.TeacherUserId = slot.TeacherUserId;
        this.GuardianUserId = guardianUserId;
        this.StudentId = studentId;
        this.StartsAt = slot.StartsAt;
        this.EndsAt = slot.EndsAt;
        this.Topic = topic;
        this.Status = AppointmentStatus.Pending;
        this.RequestedAt = now;
    }

    private Appointment()
    {
        this.Topic = string.Empty;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid SlotId { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public Guid GuardianUserId { get; private set; }

    public Guid StudentId { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    public string Topic { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>Konu geçerli mi: boşluklar kırpılınca boş değil ve en fazla 300 karakter.</summary>
    public static bool IsTopicValid(string? topic) => !string.IsNullOrWhiteSpace(topic) && topic.Trim().Length <= TopicMax;

    /// <summary>Neden geçerli mi: boş olabilir; doluysa kırpılınca en fazla 300 karakter.</summary>
    public static bool IsReasonValid(string? reason) => string.IsNullOrWhiteSpace(reason) || reason.Trim().Length <= ReasonMax;

    /// <summary>Boş ve başlamamış dilime talep (dilimin boşluğu işleyicide, öğretmen sırası içinde denetlenir).</summary>
    public static Appointment Request(AvailabilitySlot slot, Guid guardianUserId, Guid studentId, string topic, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(slot);
        if (slot.RemovedAt is not null || slot.HasStarted(now) || !IsTopicValid(topic))
        {
            throw new ArgumentOutOfRangeException(nameof(slot), "Dilim başlamamış ve açık, konu geçerli olmalı.");
        }

        return new(Guid.CreateVersion7(now), slot, guardianUserId, studentId, topic.Trim(), now);
    }

    public bool HasStarted(DateTimeOffset now) => this.StartsAt <= now;

    /// <summary>Açık randevu mu: onay bekleyen ya da onaylı ve başlamamış.</summary>
    public bool IsOpenAt(DateTimeOffset now) => this.Status is AppointmentStatus.Pending or AppointmentStatus.Approved && !this.HasStarted(now);

    /// <summary>Ekranda görünen durum ("Süresi doldu" hesaplanır).</summary>
    public AppointmentState StateAt(DateTimeOffset now) =>
        this.Status == AppointmentStatus.Pending && this.HasStarted(now) ? AppointmentState.Expired : (AppointmentState)(int)this.Status;

    public bool CanRespond(DateTimeOffset now) => this.Status == AppointmentStatus.Pending && !this.HasStarted(now);

    public bool CanCancelByGuardian(DateTimeOffset now) => this.IsOpenAt(now);

    public bool CanCancelByTeacher(DateTimeOffset now) => this.Status == AppointmentStatus.Approved && !this.HasStarted(now);

    public void Approve(DateTimeOffset now)
    {
        Ensure(this.CanRespond(now));
        this.Status = AppointmentStatus.Approved;
        this.RespondedAt = now;
    }

    public void Reject(string? reason, DateTimeOffset now)
    {
        Ensure(this.CanRespond(now) && IsReasonValid(reason));
        this.Status = AppointmentStatus.Rejected;
        this.Reason = Clean(reason);
        this.RespondedAt = now;
    }

    public void CancelByGuardian(DateTimeOffset now)
    {
        Ensure(this.CanCancelByGuardian(now));
        this.Status = AppointmentStatus.CancelledByGuardian;
        this.CancelledAt = now;
    }

    public void CancelByTeacher(string? reason, DateTimeOffset now)
    {
        Ensure(this.CanCancelByTeacher(now) && IsReasonValid(reason));
        this.Status = AppointmentStatus.CancelledByTeacher;
        this.Reason = Clean(reason);
        this.CancelledAt = now;
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static void Ensure(bool allowed)
    {
        if (!allowed)
        {
            throw new InvalidOperationException("Randevu bu duruma geçemez.");
        }
    }
}
