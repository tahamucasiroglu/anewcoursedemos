using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing;

/// <summary>
/// Tahsilat doğrulayıcılarının ortak alan kuralları (spec 0015 gereksinim 1, 2, 5; iki katmanlı doğrulamanın birinci katmanı — ikincisi
/// veritabanı <c>CHECK</c>). Her alanda ilk hata durdurur; mesajlar YAML'dan. Ad: zorunlu, kırpılmış en fazla 100. Tutar (ham metin):
/// zorunlu, sayı, sıfırdan büyük, en fazla iki ondalık (tarifede ayrıca en fazla 10.000.000,00). Yüzde (ham metin): 1–100 tam sayı.
/// </summary>
public static class BillingRules
{
    public static IRuleBuilderOptions<T, string?> BillingName<T>(this IRuleBuilderInitial<T, string?> rule, ILocalizationService localization) =>
        rule.Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage(_ => localization.BillingText(BillingMessages.NameRequired))
            .Must(s => s!.Trim().Length <= FeeSchedule.NameMax).WithMessage(_ => localization.BillingText(BillingMessages.NameTooLong));

    public static IRuleBuilderOptions<T, string?> BillingAmount<T>(this IRuleBuilderInitial<T, string?> rule, ILocalizationService localization, bool feeLimit) =>
        rule.Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage(_ => localization.BillingText(BillingMessages.AmountRequired))
            .Must(s => MoneyText.TryParseAmount(s, out _)).WithMessage(_ => localization.BillingText(BillingMessages.AmountInvalid))
            .Must(s => Parse(s) > 0m).WithMessage(_ => localization.BillingText(BillingMessages.AmountNotPositive))
            .Must(s => MoneyRules.HasAtMostTwoDecimals(Parse(s))).WithMessage(_ => localization.BillingText(BillingMessages.AmountTooManyDecimals))
            .Must(s => !feeLimit || Parse(s) <= MoneyRules.MaxFeeAmount).WithMessage(_ => localization.BillingText(BillingMessages.AmountTooLarge));

    public static IRuleBuilderOptions<T, string?> BillingPercent<T>(this IRuleBuilderInitial<T, string?> rule, ILocalizationService localization) =>
        rule.Cascade(CascadeMode.Stop)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage(_ => localization.BillingText(BillingMessages.PercentRequired))
            .Must(s => MoneyText.TryParsePercent(s, out int percent) && DiscountDefinition.IsValidPercent(percent))
            .WithMessage(_ => localization.BillingText(BillingMessages.PercentInvalid));

    /// <summary>Doğrulanmış tutar metninin değeri (doğrulamadan geçmemiş metinde 0).</summary>
    public static decimal Parse(string? text) => MoneyText.TryParseAmount(text, out decimal amount) ? amount : 0m;

    /// <summary>Doğrulanmış yüzde metninin değeri.</summary>
    public static int ParsePercent(string? text) => MoneyText.TryParsePercent(text, out int percent) ? percent : 0;
}
