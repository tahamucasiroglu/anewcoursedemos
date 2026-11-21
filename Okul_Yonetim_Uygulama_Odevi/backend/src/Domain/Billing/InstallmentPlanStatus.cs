namespace OkulYonetim.Domain.Billing;

/// <summary>Taksit planının durumu (spec 0015 gereksinim 4): geçerli ya da iptal (iptal plan cariye, gecikmeye ve hatırlatmaya girmez).</summary>
public enum InstallmentPlanStatus
{
    Active = 1,
    Cancelled = 2,
}
