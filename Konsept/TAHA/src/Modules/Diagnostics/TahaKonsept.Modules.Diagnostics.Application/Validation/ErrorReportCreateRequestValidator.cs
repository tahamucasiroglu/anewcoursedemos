using FluentValidation;
using TahaKonsept.Modules.Diagnostics.Domain.Entities;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Validation;

/// <summary>
/// <see cref="ErrorReportCreateRequest"/> doğrulaması: açıklama zorunlu (≤ 2000), kaynak tanımlı, isteğe bağlı alanlar uzunluk sınırında.
/// </summary>
public sealed class ErrorReportCreateRequestValidator : AbstractValidator<ErrorReportCreateRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ErrorReportCreateRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().WithMessage("Ne olduğunu kısaca yazın.")
            .MaximumLength(ErrorReport.MaxMessageLength).WithMessage($"Açıklama en çok {ErrorReport.MaxMessageLength} karakter olabilir.");
        RuleFor(x => x.Source).IsInEnum().WithMessage("Geçersiz kaynak.");
        RuleFor(x => x.Details).MaximumLength(ErrorReport.MaxDetailsLength).WithMessage($"Ayrıntı en çok {ErrorReport.MaxDetailsLength} karakter olabilir.");
        RuleFor(x => x.PageUrl).MaximumLength(ErrorReport.MaxPageUrlLength).WithMessage("Sayfa adresi çok uzun.");
        RuleFor(x => x.UserAgent).MaximumLength(ErrorReport.MaxUserAgentLength).WithMessage("Tarayıcı bilgisi çok uzun.");
        RuleFor(x => x.AppVersion).MaximumLength(ErrorReport.MaxAppVersionLength).WithMessage("Sürüm bilgisi çok uzun.");
        RuleFor(x => x.Machine).MaximumLength(ErrorReport.MaxMachineLength).WithMessage("Makine adı çok uzun.");
    }
}
