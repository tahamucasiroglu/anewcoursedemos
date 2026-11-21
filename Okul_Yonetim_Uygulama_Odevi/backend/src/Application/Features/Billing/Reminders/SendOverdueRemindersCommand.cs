using NArchitecture.Core.Mediator;

namespace OkulYonetim.Application.Features.Billing.Reminders;

/// <summary>
/// Seçili okulun bugünkü gecikme hatırlatmalarını gönderir (spec 0015 gereksinim 9); sonuç gönderilen hatırlatma (taksit) sayısı. Uç
/// yoktur: yalnızca arka plan işçisi okul başına sistem oturumuyla (<see cref="OverdueReminderRunner"/>) çağırır.
/// </summary>
public sealed record SendOverdueRemindersCommand : IRequest<int>;
