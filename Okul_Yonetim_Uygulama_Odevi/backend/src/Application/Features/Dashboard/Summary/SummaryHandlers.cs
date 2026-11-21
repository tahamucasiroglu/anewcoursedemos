using NArchitecture.Core.Mediator;
using OkulYonetim.Application.Features.Attendance;
using OkulYonetim.Application.Features.Billing.Reports;
using OkulYonetim.Application.Features.Communication.Announcements;
using OkulYonetim.Application.Features.Organization;

namespace OkulYonetim.Application.Features.Dashboard.Summary;

/// <summary>
/// Günlük özetin üç kartı (spec 0017 gereksinim 3–5; plan A-1: kart başına ayrı sorgu — bir kartın hatası ötekileri bozmaz). Her kart önce
/// <see cref="DashboardGuard"/>'dan geçer, sonra yalnızca diğer modüllerin sorgularını çağırır (Dashboard'da depo yok — mimari test):
/// Attendance <see cref="GetAttendanceDaySummaryQuery"/>, Billing <see cref="ListStudentBalancesQuery"/> ("yalnızca gecikenler"; en eski
/// vadeden ilk 10, eşitlikte Türkçe ad), Communication <see cref="ListUnreadAnnouncementsQuery"/> (bugün dahil son 7 gün: bugünden 6 gün
/// önceki Türkiye gününün 00:00'ından itibaren — 7 gün önceki dahil değil; ilk 10).
/// </summary>
public sealed class SummaryHandlers :
    IRequestHandler<GetAttendanceCardQuery, AttendanceCard>,
    IRequestHandler<GetOverdueCardQuery, OverdueCard>,
    IRequestHandler<GetUnreadAnnouncementsCardQuery, UnreadAnnouncementsCard>
{
    public const int TopCount = 10;
    public const int UnreadWindowDays = 7;

    private readonly DashboardGuard guard;
    private readonly ISender sender;
    private readonly SchoolCalendar calendar;

    public SummaryHandlers(DashboardGuard guard, ISender sender, SchoolCalendar calendar)
    {
        this.guard = guard;
        this.sender = sender;
        this.calendar = calendar;
    }

    public async Task<AttendanceCard> Handle(GetAttendanceCardQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.guard.RequireAsync(request.BranchId, cancellationToken).ConfigureAwait(false);
        AttendanceDaySummary day = await this.sender.Send(new GetAttendanceDaySummaryQuery(request.BranchId), cancellationToken).ConfigureAwait(false);
        List<DayClassroomRow> notTaken = [.. day.Classrooms.Where(c => !c.Taken)];
        return new AttendanceCard(
            day.Today,
            day.IsSchoolDay,
            day.Classrooms.Count,
            day.Classrooms.Count - notTaken.Count,
            notTaken.Count,
            notTaken,
            day.AbsentCount,
            day.LateCount,
            day.AbsentStudents);
    }

    public async Task<OverdueCard> Handle(GetOverdueCardQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.guard.RequireAsync(request.BranchId, cancellationToken).ConfigureAwait(false);
        StudentBalances balances = await this.sender.Send(new ListStudentBalancesQuery(request.BranchId, OverdueOnly: true), cancellationToken).ConfigureAwait(false);
        return new OverdueCard(
            balances.Today,
            balances.Rows.Count,
            balances.Rows.Sum(r => r.Totals.Overdue),
            [.. balances.Rows
                .OrderBy(r => r.OldestOverdueDue)
                .Take(TopCount)
                .Select(r => new OverdueStudent(
                    r.StudentId,
                    r.FullName,
                    r.BranchName,
                    r.ClassroomName,
                    r.Totals.Overdue,
                    r.OldestOverdueDue!.Value,
                    balances.Today.DayNumber - r.OldestOverdueDue.Value.DayNumber))]);
    }

    public async Task<UnreadAnnouncementsCard> Handle(GetUnreadAnnouncementsCardQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await this.guard.RequireAsync(request.BranchId, cancellationToken).ConfigureAwait(false);
        DateOnly since = this.calendar.Today().AddDays(-(UnreadWindowDays - 1));
        UnreadAnnouncements unread = await this.sender.Send(new ListUnreadAnnouncementsQuery(this.calendar.StartOfDay(since), request.BranchId), cancellationToken).ConfigureAwait(false);
        return new UnreadAnnouncementsCard(since, unread.Rows.Count, unread.UnreadGuardianCount, [.. unread.Rows.Take(TopCount)]);
    }
}
