using FluentValidation;
using NArchitecture.Core.Localization.Abstraction;
using OkulYonetim.Domain.Attendance;

namespace OkulYonetim.Application.Features.Attendance;

/// <summary>Yoklama kaydı: gün zorunlu; öğrenci listesi zorunlu, en fazla <see cref="MaxEntries"/>, her öğrenci bir kez; durum Var/Yok/Geç.</summary>
public sealed class SaveTodayAttendanceCommandValidator : AbstractValidator<SaveTodayAttendanceCommand>
{
    public const int MaxEntries = 500;

    public SaveTodayAttendanceCommandValidator(ILocalizationService localization)
    {
        this.RuleFor(c => c.Day).NotNull().WithMessage(_ => localization.AttendanceText(AttendanceMessages.DayRequired));
        this.RuleFor(c => c.Entries).NotNull().WithMessage(_ => localization.AttendanceText(AttendanceMessages.EntriesRequired));
        this.RuleFor(c => c.Entries!.Count).LessThanOrEqualTo(MaxEntries)
            .When(c => c.Entries is not null)
            .WithMessage(_ => localization.AttendanceText(AttendanceMessages.EntriesTooMany));
        this.RuleFor(c => c.Entries)
            .Must(e => e!.All(x => x is not null) && e!.Select(x => x.StudentId).Distinct().Count() == e!.Count)
            .When(c => c.Entries is not null)
            .WithMessage(_ => localization.AttendanceText(AttendanceMessages.StudentRepeated));
        this.RuleForEach(c => c.Entries)
            .Must(e => e is not null && TryParseStatus(e.Status, out _))
            .When(c => c.Entries is not null)
            .WithMessage(_ => localization.AttendanceText(AttendanceMessages.StatusInvalid));
    }

    /// <summary>API'deki durum metni (<c>Present</c>, <c>Absent</c>, <c>Late</c>; harf farkı gözetilmez) → durum.</summary>
    public static bool TryParseStatus(string? text, out AttendanceStatus status)
    {
        status = default;
        return text is not null
            && !int.TryParse(text, out _)
            && Enum.TryParse(text, ignoreCase: true, out status)
            && Enum.IsDefined(status);
    }
}
