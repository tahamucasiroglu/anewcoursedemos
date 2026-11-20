using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Hoş geldin ekranının okunmamış duyuru ve mesaj sayıları (spec 0011 UX). Duyuru sayısı kişinin okunmamış alıcı satırları
/// (yalnızca veli olarak aldığı duyurular), mesaj sayısı taraf olduğu konuşmalardaki karşı tarafın okunmamış mesajları; okul
/// seçilmemişse ikisi de 0.
/// </summary>
public sealed class CommunicationUnreadHandler : IRequestHandler<CountMyCommunicationUnreadQuery, CommunicationUnread>
{
    private readonly ICurrentSession session;
    private readonly IAnnouncementRepository announcements;
    private readonly IConversationRepository conversations;

    public CommunicationUnreadHandler(ICurrentSession session, IAnnouncementRepository announcements, IConversationRepository conversations)
    {
        this.session = session;
        this.announcements = announcements;
        this.conversations = conversations;
    }

    public async Task<CommunicationUnread> Handle(CountMyCommunicationUnreadQuery request, CancellationToken cancellationToken)
    {
        if (this.session.TenantId is null || this.session.UserId is not { } userId)
        {
            return new CommunicationUnread(0, 0);
        }

        return new CommunicationUnread(
            await this.announcements.CountUnreadAsync(userId, cancellationToken).ConfigureAwait(false),
            await this.conversations.CountUnreadAsync(userId, cancellationToken).ConfigureAwait(false));
    }
}
