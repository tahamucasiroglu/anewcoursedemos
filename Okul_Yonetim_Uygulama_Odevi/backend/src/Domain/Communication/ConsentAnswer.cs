namespace OkulYonetim.Domain.Communication;

/// <summary>Onay formu cevabı (spec 0011 C-4): "Onaylıyorum" ya da "Onaylamıyorum".</summary>
public enum ConsentAnswer
{
    Approved = 1,
    Declined = 2,
}
