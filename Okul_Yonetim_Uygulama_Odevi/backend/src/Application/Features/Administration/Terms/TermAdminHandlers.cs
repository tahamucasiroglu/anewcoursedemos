using Microsoft.Extensions.Logging;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Application.Ports;
using OkulYonetim.Domain.Compliance;
using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Terms;

/// <summary>
/// Dönem yönetimi (spec 0008 gereksinim 4–5): ekleme, düzenleme ve yönetim listesi. Yalnızca seçili okulun kurucusu;
/// başka okulun dönemi "bulunamadı". Ad okulda benzersiz (Türkçe harf kuralıyla); günler başka dönemle çakışamaz;
/// düzenleme takvim kayıtlarını dışarıda bırakamaz. Kurallar okul başına sıra içinde denetlenir (eşzamanlı iki çakışan
/// dönemden yalnızca biri oluşur). Silme ve pasifleştirme yok.
/// </summary>
public sealed class TermAdminHandlers :
    IRequestHandler<CreateTermCommand, TermAdminView>,
    IRequestHandler<UpdateTermCommand, TermAdminView>,
    IRequestHandler<ListAdminTermsQuery, IReadOnlyList<TermAdminView>>
{
    private readonly AdminGuard guard;
    private readonly AdminAudit audit;
    private readonly ITermRepository terms;
    private readonly ICalendarEntryRepository calendarEntries;
    private readonly SchoolCalendar calendar;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILocalizationService localization;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<TermAdminHandlers> logger;

    public TermAdminHandlers(
        AdminGuard guard,
        AdminAudit audit,
        ITermRepository terms,
        ICalendarEntryRepository calendarEntries,
        SchoolCalendar calendar,
        IUnitOfWork unitOfWork,
        ILocalizationService localization,
        TimeProvider timeProvider,
        ILogger<TermAdminHandlers> logger)
    {
        this.guard = guard;
        this.audit = audit;
        this.terms = terms;
        this.calendarEntries = calendarEntries;
        this.calendar = calendar;
        this.unitOfWork = unitOfWork;
        this.localization = localization;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<TermAdminView> Handle(CreateTermCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        DateOnly start = request.StartDate!.Value;
        DateOnly end = request.EndDate!.Value;
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            IReadOnlyList<Term> all = await this.terms.ListAsync(cancellationToken).ConfigureAwait(false);
            await this.EnsureNameFreeAndNoOverlapAsync(request.Name, start, end, all, null, cancellationToken).ConfigureAwait(false);
            Term term = Term.Create(tenantId, request.Name, start, end, this.timeProvider.GetUtcNow());
            this.terms.Add(term);
            this.audit.Record(AuditAction.TermCreated, tenantId, actorId, nameof(Term), term.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Dönem eklendi: {TermId}, okul {TenantId}", term.Id, tenantId);
            return this.View(term, [.. all, term]);
        }
    }

    public async Task<TermAdminView> Handle(UpdateTermCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        (Guid tenantId, Guid actorId) = await this.guard.RequireFounderAsync().ConfigureAwait(false);
        DateOnly start = request.StartDate!.Value;
        DateOnly end = request.EndDate!.Value;
        IExclusiveScope scope = await this.guard.LockAsync(tenantId, actorId, null, cancellationToken).ConfigureAwait(false);
        await using (scope.ConfigureAwait(false))
        {
            Term term = await this.terms.FindAsync(request.Id, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(await this.localization.GetAsync(AdministrationMessages.TermNotFound).ConfigureAwait(false));
            IReadOnlyList<Term> all = await this.terms.ListAsync(cancellationToken).ConfigureAwait(false);
            await this.EnsureNameFreeAndNoOverlapAsync(request.Name, start, end, all, term.Id, cancellationToken).ConfigureAwait(false);
            if (await this.calendarEntries.AnyOutsideAsync(term.Id, start, end, cancellationToken).ConfigureAwait(false))
            {
                throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.TermLeavesEntriesOutside).ConfigureAwait(false));
            }

            term.Update(request.Name, start, end);
            this.audit.Record(AuditAction.TermUpdated, tenantId, actorId, nameof(Term), term.Id);
            await this.unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            this.logger.LogInformation("Dönem düzenlendi: {TermId}", term.Id);
            return this.View(term, all);
        }
    }

    public async Task<IReadOnlyList<TermAdminView>> Handle(ListAdminTermsQuery request, CancellationToken cancellationToken)
    {
        await this.guard.RequireFounderAsync().ConfigureAwait(false);
        IReadOnlyList<Term> all = await this.terms.ListAsync(cancellationToken).ConfigureAwait(false);
        DateOnly today = this.calendar.Today();
        Term? current = SchoolCalendar.PickCurrent(all, today);
        return [.. all.OrderByDescending(t => t.StartDate).Select(t => TermAdminView.Of(t, current, today))];
    }

    private TermAdminView View(Term term, IReadOnlyList<Term> all)
    {
        DateOnly today = this.calendar.Today();
        return TermAdminView.Of(term, SchoolCalendar.PickCurrent(all, today), today);
    }

    private async Task EnsureNameFreeAndNoOverlapAsync(string name, DateOnly start, DateOnly end, IReadOnlyList<Term> all, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await this.terms.NameExistsAsync(name, exceptId, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.TermNameExists).ConfigureAwait(false));
        }

        if (all.Any(t => t.Id != exceptId && t.Overlaps(start, end)))
        {
            throw new BusinessException(await this.localization.GetAsync(AdministrationMessages.TermOverlaps).ConfigureAwait(false));
        }
    }
}
