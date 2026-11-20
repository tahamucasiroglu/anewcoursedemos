using Microsoft.EntityFrameworkCore;
using OkulYonetim.Application.Features.Communication.Messages;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Persistence.Repositories;

/// <summary>
/// Konuşma deposu (spec 0011): konuşma ve mesaj kurum filtreli; listeler yalnızca kişinin taraf olduğu konuşmalar. Okunmamış
/// sayısı veritabanında sayılır: karşı tarafın, kişinin son okuma zamanından sonraki mesajları. Öğrenci adı öğrenci
/// filtresinden, kişi adı kişi tablosundan; okumalar izlemesiz projeksiyon (T7). Mesaj sırası gönderim zamanıdır.
/// </summary>
public sealed class ConversationRepository : IConversationRepository
{
    private readonly OkulDbContext context;

    public ConversationRepository(OkulDbContext context)
    {
        this.context = context;
    }

    public Task<Conversation?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        this.context.Conversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Conversation?> FindByPartiesAsync(Guid teacherUserId, Guid guardianUserId, Guid studentId, CancellationToken cancellationToken) =>
        this.context.Conversations.FirstOrDefaultAsync(
            c => c.TeacherUserId == teacherUserId && c.GuardianUserId == guardianUserId && c.StudentId == studentId,
            cancellationToken);

    public async Task<IReadOnlyList<ConversationRow>> ListForUserAsync(Guid userId, Guid? conversationId, int max, CancellationToken cancellationToken)
    {
        IQueryable<Conversation> query = this.context.Conversations.AsNoTracking().Where(c => c.TeacherUserId == userId || c.GuardianUserId == userId);
        if (conversationId is { } id)
        {
            query = query.Where(c => c.Id == id);
        }

        var rows = await (from c in query
                          let isTeacher = c.TeacherUserId == userId
                          let other = isTeacher ? c.GuardianUserId : c.TeacherUserId
                          let readAt = isTeacher ? c.TeacherReadAt : c.GuardianReadAt
                          orderby c.LastMessageAt descending, c.Id descending
                          select new
                          {
                              c.Id,
                              c.StudentId,
                              StudentName = this.context.Students.Where(s => s.Id == c.StudentId).Select(s => s.FullName).FirstOrDefault(),
                              Other = other,
                              OtherName = this.context.Users.Where(u => u.Id == other).Select(u => u.FullName).FirstOrDefault(),
                              IsTeacher = isTeacher,
                              c.LastMessageAt,
                              Unread = this.context.ConversationMessages.Count(m => m.ConversationId == c.Id && m.SenderUserId != userId && (readAt == null || m.SentAt > readAt)),
                          })
            .Take(max)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(r => new ConversationRow(
            r.Id,
            r.StudentId,
            r.StudentName ?? string.Empty,
            r.Other,
            r.OtherName ?? string.Empty,
            r.IsTeacher ? "Teacher" : "Guardian",
            r.LastMessageAt,
            r.Unread))];
    }

    public async Task<IReadOnlyList<ConversationMessage>> ListMessagesAsync(Guid conversationId, Guid? before, int take, CancellationToken cancellationToken)
    {
        IQueryable<ConversationMessage> query = this.context.ConversationMessages.AsNoTracking().Where(m => m.ConversationId == conversationId);
        if (before is { } beforeId)
        {
            DateTimeOffset? cursor = await this.context.ConversationMessages.AsNoTracking()
                .Where(m => m.Id == beforeId && m.ConversationId == conversationId)
                .Select(m => (DateTimeOffset?)m.SentAt)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (cursor is null)
            {
                return [];
            }

            query = query.Where(m => m.SentAt < cursor.Value);
        }

        return await query.OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id).Take(take)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken) =>
        (from c in this.context.Conversations
         where c.TeacherUserId == userId || c.GuardianUserId == userId
         let readAt = c.TeacherUserId == userId ? c.TeacherReadAt : c.GuardianReadAt
         from m in this.context.ConversationMessages
         where m.ConversationId == c.Id && m.SenderUserId != userId && (readAt == null || m.SentAt > readAt)
         select m.Id).CountAsync(cancellationToken);

    public void Add(Conversation conversation) => this.context.Conversations.Add(conversation);

    public void AddMessage(ConversationMessage message) => this.context.ConversationMessages.Add(message);
}
