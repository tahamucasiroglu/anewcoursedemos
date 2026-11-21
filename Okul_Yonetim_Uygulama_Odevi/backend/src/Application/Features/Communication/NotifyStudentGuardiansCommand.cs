using NArchitecture.Core.Mediator;
using OkulYonetim.Domain.Communication;

namespace OkulYonetim.Application.Features.Communication;

/// <summary>
/// Diğer modüllerin kayda bağlı "Okul bildirimi" kapısı (spec 0013 gereksinim 2; plan 0013 A-1): verilen öğrencilerin okulda
/// etkin her velisine, öğrenci başına bir uygulama içi bildirimi (<see cref="GuardianNotice"/>, tür + kaynak kayıt + hazır metin)
/// çağıranın işlemi içinde yazar ve gönderilecek bildirimleri döndürür (kaydetmez, göndermez; çağıran commit sonrası
/// <c>DeliverNotificationsCommand</c> ile kapıya verir). Metin öğrenci adı taşımaz. Uç yoktur; loglanmaz (T9).
/// </summary>
public sealed record NotifyStudentGuardiansCommand(IReadOnlyCollection<Guid> StudentIds, GuardianNoticeKind Kind, Guid SourceId, DateOnly Day, string Text) : IRequest<SchoolNoticeResult>;
