namespace OkulYonetim.Domain.Communication;

/// <summary>
/// Öğretmen ile veli arasındaki, bir öğrenci bağlamındaki birebir konuşma (spec 0011 C-5): okul + öğretmen + veli + öğrenci
/// tekildir. Son mesaj zamanı listeyi sıralar; iki tarafın son okuma zamanı okunmamış sayısını belirler (karşı tarafın bu
/// zamandan sonraki mesajları). Konuşmayı yalnızca iki taraf görür.
/// </summary>
public sealed class Conversation
{
    private Conversation(Guid id, Guid tenantId, Guid teacherUserId, Guid guardianUserId, Guid studentId, DateTimeOffset createdAt)
    {
        this.Id = id;
        this.TenantId = tenantId;
        this.TeacherUserId = teacherUserId;
        this.GuardianUserId = guardianUserId;
        this.StudentId = studentId;
        this.CreatedAt = createdAt;
        this.LastMessageAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid TeacherUserId { get; private set; }

    public Guid GuardianUserId { get; private set; }

    public Guid StudentId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastMessageAt { get; private set; }

    public DateTimeOffset? TeacherReadAt { get; private set; }

    public DateTimeOffset? GuardianReadAt { get; private set; }

    public static Conversation Start(Guid tenantId, Guid teacherUserId, Guid guardianUserId, Guid studentId, DateTimeOffset now) =>
        new(Guid.CreateVersion7(now), tenantId, teacherUserId, guardianUserId, studentId, now);

    public bool HasParty(Guid userId) => userId == this.TeacherUserId || userId == this.GuardianUserId;

    /// <summary>Yeni mesaj: son mesaj zamanı ilerler, gönderen kendi mesajını okumuş sayılır.</summary>
    public void Record(Guid senderUserId, DateTimeOffset now)
    {
        this.LastMessageAt = now;
        this.MarkReadBy(senderUserId, now);
    }

    /// <summary>Taraf <paramref name="upTo"/> anına kadarki mesajları gördü (okuma zamanı yalnızca ileri gider).</summary>
    public void MarkReadBy(Guid userId, DateTimeOffset upTo)
    {
        if (userId == this.TeacherUserId && (this.TeacherReadAt is null || upTo > this.TeacherReadAt))
        {
            this.TeacherReadAt = upTo;
        }

        if (userId == this.GuardianUserId && (this.GuardianReadAt is null || upTo > this.GuardianReadAt))
        {
            this.GuardianReadAt = upTo;
        }
    }
}
