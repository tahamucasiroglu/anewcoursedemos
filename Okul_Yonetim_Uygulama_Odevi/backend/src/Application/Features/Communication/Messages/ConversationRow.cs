namespace OkulYonetim.Application.Features.Communication.Messages;

/// <summary>Konuşma listesi satırı: öğrenci, karşı taraf, kişinin bu konuşmadaki rolü (<c>Teacher</c>/<c>Guardian</c>), son mesaj zamanı ve okunmamış sayısı.</summary>
public sealed record ConversationRow(Guid Id, Guid StudentId, string StudentName, Guid CounterpartUserId, string CounterpartName, string MyRole, DateTimeOffset LastMessageAt, int UnreadCount);
