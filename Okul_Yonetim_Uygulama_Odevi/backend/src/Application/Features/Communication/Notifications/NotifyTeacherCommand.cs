using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;

namespace OkulYonetim.Application.Features.Communication.Notifications;

/// <summary>
/// Öğretmene (tek kişiye) giden bildirimin kapı girişi (spec 0016 C-5; plan 0016 Risks 2 / A-3): diğer modüller kayıtlarını commit
/// ettikten <b>sonra</b> çağırır. Öğretmenin mesai saatleri (0011 <c>QuietHours</c>, BR-8) burada uygulanır: tanımlıysa ve şu an
/// (Türkiye saati) mesai dışındaysa bildirim gitmez — birikmiş bildirim de sonra gönderilmez; tanımsızsa her zaman gider. Sonuç
/// bildirimin kapıya verilip verilmediğidir. Metin yalnızca tür, gün ve saat taşır (ad ve konu yok); uç yoktur, loglanmaz (T9).
/// </summary>
public sealed record NotifyTeacherCommand(Guid TeacherUserId, PushNotificationKind Kind, Guid SourceId, string Text) : IRequest<bool>;
