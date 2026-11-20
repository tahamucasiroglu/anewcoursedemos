using OkulYonetim.Application.Features.Communication.Messages;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Ports;

/// <summary>
/// Öğretmen–veli konuşmaları ve mesajları (spec 0011). Kurum filtreli; konuşmayı yalnızca iki taraf görür — liste ve satır
/// okumaları verilen kişinin taraf olduğu konuşmalarla süzülür. Okunmamış sayısı: karşı tarafın, kişinin son okumasından
/// sonraki mesajları. Listeler izlemesiz projeksiyon (T7).
/// </summary>
public interface IConversationRepository
{
    /// <summary>Konuşma (izlenen); kişi taraf değilse de döner — taraf denetimi çağıranda.</summary>
    Task<Conversation?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<Conversation?> FindByPartiesAsync(Guid teacherUserId, Guid guardianUserId, Guid studentId, CancellationToken cancellationToken);

    /// <summary>Kişinin taraf olduğu konuşmalar, son mesaja göre yeniden eskiye; <paramref name="conversationId"/> verilirse yalnızca o.</summary>
    Task<IReadOnlyList<ConversationRow>> ListForUserAsync(Guid userId, Guid? conversationId, int max, CancellationToken cancellationToken);

    /// <summary>Konuşmanın mesajları yeniden eskiye: <paramref name="before"/> verilirse o mesajdan öncekiler; en fazla <paramref name="take"/>.</summary>
    Task<IReadOnlyList<ConversationMessage>> ListMessagesAsync(Guid conversationId, Guid? before, int take, CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Conversation conversation);

    void AddMessage(ConversationMessage message);
}
