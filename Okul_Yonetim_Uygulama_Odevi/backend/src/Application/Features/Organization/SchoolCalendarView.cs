using OkulYonetim.Application.Features.Administration.Calendar;
using OkulYonetim.Application.Features.Administration.Terms;

namespace OkulYonetim.Application.Features.Organization;

/// <summary>
/// Okul takvimi (spec 0008 gereksinim 8, herkes salt okunur): güncel ya da yaklaşan dönem (yoksa boş) ve o dönemin
/// takvim kayıtları, başlangıç gününe göre sıralı.
/// </summary>
public sealed record SchoolCalendarView(TermAdminView? Term, IReadOnlyList<CalendarEntryView> Entries);
