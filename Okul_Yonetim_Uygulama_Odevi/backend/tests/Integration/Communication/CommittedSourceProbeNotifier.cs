using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OkulYonetim.Application.Ports;
using OkulYonetim.Persistence.Contexts;

namespace OkulYonetim.Tests.Integration.Communication;

/// <summary>
/// Bildirim anında kaynağın (duyuru, mesaj, okul bildirimi) veritabanında kalıcı olup olmadığını <b>ayrı bir bağlantıdan</b>
/// soran gönderici (spec 0011 AC-10, plan Risks 4 — VERIFY V-3): bildirim kayıttan/commit'ten önce çıkarsa kaynak satır
/// başka bağlantıya henüz görünmez ve <see cref="Deliveries"/>'te <c>Committed = false</c> olur.
/// </summary>
public sealed class CommittedSourceProbeNotifier : IPushNotifier
{
    /// <summary>API ayağa kalktıktan sonra testin verdiği servis sağlayıcı (her soru kendi kapsamında, kendi bağlantısıyla).</summary>
    public IServiceProvider? Services { get; set; }

    public ConcurrentQueue<(PushNotification Notification, bool Committed)> Deliveries { get; } = new();

    public async Task NotifyAsync(PushNotification notification, CancellationToken cancellationToken)
    {
        IServiceProvider services = this.Services ?? throw new InvalidOperationException("Services verilmedi");
        using IServiceScope scope = services.CreateScope();
        OkulDbContext context = scope.ServiceProvider.GetRequiredService<OkulDbContext>();
        Guid id = notification.SourceId;
        bool committed = notification.Kind switch
        {
            PushNotificationKind.Announcement or PushNotificationKind.ConsentForm =>
                await context.Announcements.IgnoreQueryFilters().AnyAsync(a => a.Id == id, cancellationToken),
            PushNotificationKind.Message => await context.ConversationMessages.IgnoreQueryFilters().AnyAsync(m => m.Id == id, cancellationToken),
            PushNotificationKind.SchoolNotice => await context.GuardianNotices.IgnoreQueryFilters().AnyAsync(n => n.Id == id, cancellationToken),
            _ => false,
        };
        this.Deliveries.Enqueue((notification, committed));
    }
}
