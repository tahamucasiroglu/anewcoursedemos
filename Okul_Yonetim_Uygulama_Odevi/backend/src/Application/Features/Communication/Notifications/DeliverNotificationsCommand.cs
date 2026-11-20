using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Communication.Notifications;

/// <summary>Bildirim kapısının commit sonrası adımı (spec 0011 gereksinim 7, backlog B-30): kayıt kalıcı olduktan sonra bildirimleri göndericiye verir ve e-postaları kuyruğa bırakır. Diğer modüller (yoklama) veliye giden her şeyi bu komutla yollar; uç yoktur, kişisel veri taşıdığı için loglanmaz (T9).</summary>
public sealed record DeliverNotificationsCommand(IReadOnlyList<PushNotification> Pushes, IReadOnlyList<EmailMessage> Emails) : IRequest<int>;
