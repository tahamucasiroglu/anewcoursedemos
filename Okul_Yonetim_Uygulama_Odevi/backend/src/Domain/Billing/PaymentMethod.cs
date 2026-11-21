namespace OkulYonetim.Domain.Billing;

/// <summary>Tahsilat yöntemi (spec 0015 C-1, C-9): nakit, havale/EFT (banka/IBAN bilgisi girilmez), okulun kendi POS cihazıyla kredi kartı (kart bilgisi sisteme girmez).</summary>
public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    CardPos = 3,
}
