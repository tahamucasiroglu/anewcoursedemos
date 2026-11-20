using System.Globalization;
using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Administration;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Communication;
using OkulYonetim.Domain.Compliance;

namespace OkulYonetim.Application.Features.Communication.WorkingHours;

/// <summary>
/// Öğretmenin mesai saatleri (spec 0011 gereksinim 6, 9): yalnızca seçili okulda Öğretmen rolü olan kişi (değilse 403) kendi
/// saatlerini okur, kaydeder ya da sessiz modu kapatır (satır silinir). Kayıt kişi başına sıradadır
/// (<c>quiet-hours:&lt;okul&gt;:&lt;kişi&gt;</c>); her değişiklik audit'e yazılır (yeni değerler ayrıntıda).
/// </summary>
public sealed class QuietHoursHandlers :
    IRequestHandler<GetMyQuietHoursQuery, QuietHoursView>,
    IRequestHandler<SetMyQuietHoursCommand, QuietHoursView>
{
    private static readonly QuietHoursView Disabled = new(false, null, null, []);

    private readonly CommunicationGuard guard;
    private readonly IQuietHoursRepository quietHours;
    private readonly AdminAudit audit;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<QuietHoursHandlers> logger;

    public QuietHoursHandlers(
        CommunicationGuard guard,
        IQuietHoursRepository quietHours,
        AdminAudit audit,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<QuietHoursHandlers> logger)
    {
        this.guard = guard;
        this.quietHours = quietHours;
        this.audit = audit;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<QuietHoursView> Handle(GetMyQuietHoursQuery request, CancellationToken cancellationToken)
    {
        CommunicationCaller caller = await this.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        return View(await this.quietHours.FindAsync(caller.UserId, cancellationToken).ConfigureAwait(false));
    }

    public async Task<QuietHoursView> Handle(SetMyQuietHoursCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommunicationCaller caller = await this.RequireTeacherAsync(cancellationToken).ConfigureAwait(false);
        IExclusiveScope scope;
        try
        {
            scope = await this.unitOfWork.BeginExclusiveAsync("quiet-hours:" + caller.TenantId.ToString("N") + ":" + caller.UserId.ToString("N"), cancellationToken).ConfigureAwait(false);
        }
        catch (ExclusiveLockTimeoutException)
        {
            throw new BusinessException(await this.localization.CommunicationTextAsync(CommunicationMessages.TooManyRequests).ConfigureAwait(false));
        }

        QuietHours? current;
        await using (scope.ConfigureAwait(false))
        {
            DateTimeOffset now = this.timeProvider.GetUtcNow();
            current = await this.quietHours.FindAsync(caller.UserId, cancellationToken).ConfigureAwait(false);
            string detail;
            if (!request.Enabled)
            {
                if (current is not null)
                {
                    this.quietHours.Remove(current);
                    current = null;
                }

                detail = "enabled=false";
            }
            else
            {
                if (!QuietHoursText.TryParseTime(request.Start, out TimeOnly start)
                    || !QuietHoursText.TryParseTime(request.End, out TimeOnly end)
                    || !QuietHoursText.TryParseDays(request.Days, out WorkDays days))
                {
                    throw new InvalidOperationException("Mesai saatleri doğrulayıcıdan geçmeden kaydedilemez.");
                }

                if (current is null)
                {
                    current = QuietHours.Define(caller.TenantId, caller.UserId, start, end, days, now);
                    this.quietHours.Add(current);
                }
                else
                {
                    current.Change(start, end, days, now);
                }

                detail = string.Create(CultureInfo.InvariantCulture, $"enabled=true;start={QuietHoursText.Format(start)};end={QuietHoursText.Format(end)};days={(int)days}");
            }

            this.audit.Record(AuditAction.QuietHoursChanged, caller.TenantId, caller.UserId, nameof(QuietHours), caller.UserId, detail);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        this.logger.LogInformation("Mesai saatleri değişti: öğretmen {UserId}, sessiz mod {Enabled}", caller.UserId, request.Enabled);
        return View(current);
    }

    private static QuietHoursView View(QuietHours? hours) =>
        hours is null ? Disabled : new QuietHoursView(true, QuietHoursText.Format(hours.Start), QuietHoursText.Format(hours.End), QuietHoursText.DayNames(hours.Days));

    private async Task<CommunicationCaller> RequireTeacherAsync(CancellationToken cancellationToken)
    {
        CommunicationCaller caller = await this.guard.CallerAsync(cancellationToken).ConfigureAwait(false);
        return caller.IsTeacher ? caller : throw await this.guard.ForbiddenAsync().ConfigureAwait(false);
    }
}
