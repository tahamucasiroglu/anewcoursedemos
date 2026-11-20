using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Calendar;

/// <summary>
/// Akademik takvim yönetimi (spec 0008 gereksinim 7): dönemin takvimine kayıt ekleme, düzenleme, silme (yumuşak) ve
/// yönetim listesi. Yalnızca seçili okulun kurucusu; başka okulun dönemi/kaydı "bulunamadı". Kaydın iki günü de dönemin
/// içinde olmalı; kural, dönem tarih düzenlemesiyle aynı okul sırasında denetlenir. Her değişiklik audit'li.
/// </summary>
public sealed class CalendarAdminHandlers :
    IRequestHandler<CreateCalendarEntryCommand, CalendarEntryView>,
    IRequestHandler<UpdateCalendarEntryCommand, CalendarEntryView>,
    IRequestHandler<DeleteCalendarEntryCommand, bool>,
    IRequestHandler<ListTermCalendarQuery, IReadOnlyList<CalendarEntryView>>
{
    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly ITermRepository terms;
    private readonly ICalendarEntryRepository entries;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<CalendarAdminHandlers> logger;

    public CalendarAdminHandlers(
        AdminGuard guard,
        AdminAudit audit,
        ITermRepository terms,
        ICalendarEntryRepository entries,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<CalendarAdminHandlers> logger)
    {
        this.guard = guard;
        this.audit = audit;
        this.terms = terms;
        this.entries = entries;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<CalendarEntryView> Handle(CreateCalendarEntryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        CalendarEntryKind kind = CalendarEntryFormRules.ParseKind(request.Kind);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Term term = await this.FindTermAsync(request.TermId, cancellationToken).ConfigureAwait(false);
            await this.EnsureInsideAsync(term, request.StartDate!.Value, request.EndDate!.Value).ConfigureAwait(false);
            CalendarEntry entry = CalendarEntry.Create(tenantId, term.Id, request.Title, kind, request.StartDate.Value, request.EndDate.Value, this.timeProvider.GetUtcNow());
            this.entries.Add(entry);
            this.audit.Record(AuditAction.CalendarEntryCreated, tenantId, actorId, nameof(CalendarEntry), entry.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Takvim kaydı eklendi: {CalendarEntryId}, dönem {TermId}", entry.Id, term.Id);
            return CalendarEntryView.Of(entry);
        }
    }

    public async Task<CalendarEntryView> Handle(UpdateCalendarEntryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        CalendarEntryKind kind = CalendarEntryFormRules.ParseKind(request.Kind);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            CalendarEntry entry = await this.FindEntryAsync(request.Id, cancellationToken).ConfigureAwait(false);
            Term term = await this.FindTermAsync(entry.TermId, cancellationToken).ConfigureAwait(false);
            await this.EnsureInsideAsync(term, request.StartDate!.Value, request.EndDate!.Value).ConfigureAwait(false);
            entry.Update(request.Title, kind, request.StartDate.Value, request.EndDate.Value);
            this.audit.Record(AuditAction.CalendarEntryUpdated, tenantId, actorId, nameof(CalendarEntry), entry.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Takvim kaydı düzenlendi: {CalendarEntryId}", entry.Id);
            return CalendarEntryView.Of(entry);
        }
    }

    public async Task<bool> Handle(DeleteCalendarEntryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            CalendarEntry entry = await this.FindEntryAsync(request.Id, cancellationToken).ConfigureAwait(false);
            entry.Delete(this.timeProvider.GetUtcNow());
            this.audit.Record(AuditAction.CalendarEntryDeleted, tenantId, actorId, nameof(CalendarEntry), entry.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Takvim kaydı silindi: {CalendarEntryId}", entry.Id);
            return true;
        }
    }

    public async Task<IReadOnlyList<CalendarEntryView>> Handle(ListTermCalendarQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.guard.RequireFounderAsync().ConfigureAwait(false);
        Term term = await this.FindTermAsync(request.TermId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<CalendarEntry> list = await this.entries.ListByTermAsync(term.Id, cancellationToken).ConfigureAwait(false);
        return [.. list.Select(CalendarEntryView.Of)];
    }

    private async Task<Term> FindTermAsync(Guid id, CancellationToken cancellationToken) =>
        await this.terms.FindAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.TermNotFound).ConfigureAwait(false));

    private async Task<CalendarEntry> FindEntryAsync(Guid id, CancellationToken cancellationToken) =>
        await this.entries.FindAsync(id, cancellationToken).ConfigureAwait(false)
        ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.CalendarEntryNotFound).ConfigureAwait(false));

    private async Task EnsureInsideAsync(Term term, DateOnly start, DateOnly end)
    {
        if (!term.Contains(start) || !term.Contains(end))
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.CalendarEntryOutsideTerm).ConfigureAwait(false));
        }
    }
}
