using OkulYonetim.Domain.Organization;

namespace OkulYonetim.Application.Features.Administration.Terms;

/// <summary>
/// Dönem görünümü (yönetim listesi ve okul takvimi): ad, başlangıç ve bitiş günü; güncel dönemse <c>IsCurrent</c>, henüz
/// başlamamış en yakın dönemse <c>IsUpcoming</c> (spec 0008 gereksinim 5). İkisi aynı anda doğru olmaz.
/// </summary>
public sealed record TermAdminView(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent, bool IsUpcoming)
{
    public static TermAdminView Of(Term term, Term? current, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(term);
        bool selected = current is not null && current.Id == term.Id;
        return new(term.Id, term.Name, term.StartDate, term.EndDate, selected && term.Contains(today), selected && term.StartDate > today);
    }
}
