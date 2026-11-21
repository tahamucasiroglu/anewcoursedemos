using NArchitecture.Core.Localization.Abstraction;

namespace OkulYonetim.Application.Features.Billing;

/// <summary>
/// Tahsilat ve ön muhasebe (spec 0015) metinlerinin anahtarları ve okuyucusu (YAML: <c>Features/Billing/Resources/Locales/billing.tr.yaml</c>).
/// Metin koda gömülmez; doğrulayıcılar eşzamanlı okuyucuyu kullanır. Hatırlatma metni <c>{student}</c>, <c>{due}</c> (gün.ay.yıl) ve
/// <c>{remaining}</c> ("1.234,50") yer tutucularını taşır.
/// </summary>
public static class BillingMessages
{
    public const string Section = "Billing";
    public const string Forbidden = "Forbidden";
    public const string FounderOnly = "FounderOnly";
    public const string StudentNotFound = "StudentNotFound";
    public const string FeeScheduleNotFound = "FeeScheduleNotFound";
    public const string DiscountNotFound = "DiscountNotFound";
    public const string PlanNotFound = "PlanNotFound";
    public const string PaymentNotFound = "PaymentNotFound";
    public const string NameRequired = "NameRequired";
    public const string NameTooLong = "NameTooLong";
    public const string AmountRequired = "AmountRequired";
    public const string AmountInvalid = "AmountInvalid";
    public const string AmountNotPositive = "AmountNotPositive";
    public const string AmountTooManyDecimals = "AmountTooManyDecimals";
    public const string AmountTooLarge = "AmountTooLarge";
    public const string PercentRequired = "PercentRequired";
    public const string PercentInvalid = "PercentInvalid";
    public const string FeeNameTaken = "FeeNameTaken";
    public const string DiscountNameTaken = "DiscountNameTaken";
    public const string StudentRequired = "StudentRequired";
    public const string FeeScheduleRequired = "FeeScheduleRequired";
    public const string InstallmentCountInvalid = "InstallmentCountInvalid";
    public const string FirstDueRequired = "FirstDueRequired";
    public const string FirstDueOutOfRange = "FirstDueOutOfRange";
    public const string StudentLeft = "StudentLeft";
    public const string FeeScheduleInactive = "FeeScheduleInactive";
    public const string DiscountInactive = "DiscountInactive";
    public const string NetZero = "NetZero";
    public const string InstallmentTooSmall = "InstallmentTooSmall";
    public const string PlanExists = "PlanExists";
    public const string PlanHasPayments = "PlanHasPayments";
    public const string PlanAlreadyCancelled = "PlanAlreadyCancelled";
    public const string PlanCancelled = "PlanCancelled";
    public const string PlanRequired = "PlanRequired";
    public const string AmountExceedsRemaining = "AmountExceedsRemaining";
    public const string DateRequired = "DateRequired";
    public const string DateInFuture = "DateInFuture";
    public const string DateTooOld = "DateTooOld";
    public const string MethodRequired = "MethodRequired";
    public const string NoteTooLong = "NoteTooLong";
    public const string ReasonRequired = "ReasonRequired";
    public const string ReasonTooLong = "ReasonTooLong";
    public const string PaymentAlreadyCancelled = "PaymentAlreadyCancelled";
    public const string TooManyRequests = "TooManyRequests";
    public const string PageInvalid = "PageInvalid";
    public const string SearchTooLong = "SearchTooLong";
    public const string ReminderNotice = "ReminderNotice";

    private static readonly string[] Locales = ["tr"];

    public static Task<string> BillingTextAsync(this ILocalizationService localization, string key)
    {
        ArgumentNullException.ThrowIfNull(localization);
        return localization.GetLocalizedAsync(key, Locales, Section);
    }

    public static string BillingText(this ILocalizationService localization, string key) =>
        localization.BillingTextAsync(key).GetAwaiter().GetResult();
}
