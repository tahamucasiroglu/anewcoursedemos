using FluentValidation;
using TahaKonsept.Shared.Contracts.Diagnostics;

namespace TahaKonsept.Modules.Diagnostics.Application.Validation;

/// <summary>
/// <see cref="ErrorReportStatusUpdateRequest"/> doğrulaması: durum tanımlı bir değer olmalı.
/// </summary>
public sealed class ErrorReportStatusUpdateRequestValidator : AbstractValidator<ErrorReportStatusUpdateRequest>
{
    /// <summary>Kuralları tanımlar.</summary>
    public ErrorReportStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Geçersiz durum.");
    }
}
