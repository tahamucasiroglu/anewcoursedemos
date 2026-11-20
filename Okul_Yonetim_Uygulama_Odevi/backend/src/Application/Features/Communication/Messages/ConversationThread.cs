namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Konuşma ekranı: konuşma satırı, yeni mesaj gönderilebilir mi (ilişki sürüyor mu), mesajlar (eskiden yeniye) ve daha eski mesaj var mı.</summary>
public sealed record ConversationThread(ConversationRow Conversation, bool CanSend, IReadOnlyList<MessageView> Messages, bool HasMore);
