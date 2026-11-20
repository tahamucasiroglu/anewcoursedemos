namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Konuşma bağlamı (spec 0012): öğrenci, çağıran konuşmanın öğretmeni mi ve öğretmen–veli–öğrenci ilişkisi sürüyor mu (yalnızca öğretmen için hesaplanır).</summary>
public sealed record TeacherConversationContext(Guid ConversationId, Guid StudentId, bool IsTeacher, bool CanSend);
