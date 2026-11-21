using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Application.Features.Organization;
using OkulYonetim.Domain.Billing;

namespace OkulYonetim.Application.Features.Billing.Payments;

/// <summary>
/// Tahsilat kaydı (spec 0015 AC-6): plan zorunlu; tutar sıfırdan büyük, en fazla 2 ondalık (kalan borç sınırı işleyicide, sıra içinde);
/// tarih zorunlu, gelecek olamaz ve en fazla 365 gün önce (Türkiye günü); yöntem <c>Cash</c>, <c>BankTransfer</c> ya da <c>CardPos</c>;
/// not en fazla 200 karakter (kırpılmış).
/// </summary>
public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator(ILocalizationService localization, SchoolCalendar calendar)
    {
        ArgumentNullException.ThrowIfNull(calendar);
        this.RuleFor(c => c.PlanId).NotEqual(Guid.Empty).WithMessage(_ => localization.BillingText(BillingMessages.PlanRequired));
        this.RuleFor(c => c.Amount).BillingAmount(localization, feeLimit: false);
        this.RuleFor(c => c.Date).Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(_ => localization.BillingText(BillingMessages.DateRequired))
            .Must(d => d!.Value <= calendar.Today()).WithMessage(_ => localization.BillingText(BillingMessages.DateInFuture))
            .Must(d => Payment.IsDateAllowed(d!.Value, calendar.Today())).WithMessage(_ => localization.BillingText(BillingMessages.DateTooOld));
        this.RuleFor(c => c.Method).Must(m => TryParseMethod(m, out _)).WithMessage(_ => localization.BillingText(BillingMessages.MethodRequired));
        this.RuleFor(c => c.Note).Must(n => n!.Trim().Length <= Payment.NoteMax).When(c => !string.IsNullOrWhiteSpace(c.Note))
            .WithMessage(_ => localization.BillingText(BillingMessages.NoteTooLong));
    }

    /// <summary>Yöntem adı birebir (büyük/küçük harf ve sayı kabul edilmez).</summary>
    public static bool TryParseMethod(string? text, out PaymentMethod method)
    {
        method = default;
        return text is not null && Enum.GetNames<PaymentMethod>().Contains(text, StringComparer.Ordinal) && Enum.TryParse(text, false, out method);
    }
}
