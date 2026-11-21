namespace OkulYonetim.Application.Features.Organization;

/// <summary>Dönemin kısa bilgisi (spec 0013): kimlik, ad, başlangıç ve bitiş günü (uçlar dahil).</summary>
public sealed record TermSummary(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate)
{
    public bool Contains(DateOnly day) => day >= this.StartDate && day <= this.EndDate;
}
