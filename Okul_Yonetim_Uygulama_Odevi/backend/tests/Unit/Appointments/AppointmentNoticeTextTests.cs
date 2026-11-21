using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection;
using OkulYonetim.Application.Features.Appointments;
using OkulYonetim.Domain.Appointments;
using OkulYonetim.Domain.Communication;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Appointments;

/// <summary>
/// Spec 0016 review tur 1 R1-1: veliye giden randevu bildiriminin metni gerçek <c>appointments.tr.yaml</c> şablonlarından en uzun öğretmen
/// adı (200 — <c>UserConfiguration</c>'daki <c>full_name</c> sınırı), en uzun neden (<see cref="Appointment.ReasonMax"/>) ve iki haneli gün/saatle
/// doldurulunca <see cref="GuardianNotice.AppointmentTextMax"/>'ı aşmaz ve <see cref="GuardianNotice.ForRecord"/> istisna atmaz (aşsaydı onay,
/// ret ya da öğretmen iptali commit'ten önce düşerdi). Şablon listesi YAML'dan okunur (<c>{teacher}</c> içeren her anahtar = veli şablonu):
/// yeni bir veli şablonu eklenip burada sınanmazsa test kırmızı olur.
/// </summary>
public sealed partial class AppointmentNoticeTextTests
{
    /// <summary>Kullanıcının ad-soyad kolon sınırı (<c>UserConfiguration</c>: <c>FullName</c> HasMaxLength(200)).</summary>
    private const int TeacherNameMax = 200;

    private static readonly string[] GuardianTemplates =
    [
        AppointmentsMessages.ApprovedNotice,
        AppointmentsMessages.RejectedNotice,
        AppointmentsMessages.RejectedWithReasonNotice,
        AppointmentsMessages.TeacherCancelledNotice,
        AppointmentsMessages.TeacherCancelledWithReasonNotice,
    ];

    /// <summary>İki haneli gün, ay ve saat (Türkiye saati) — gün/saat parçasının en uzun hâli.</summary>
    private static readonly DateTimeOffset TurkeyStart = new(2026, 12, 28, 23, 45, 0, TimeSpan.FromHours(3));

    public static TheoryData<string> Templates() => [.. GuardianTemplates];

    [Fact]
    public void GuardianTemplates_CoverEveryTeacherTemplateInYaml()
    {
        string yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Features", "Appointments", "Resources", "Locales", "appointments.tr.yaml"));
        string[] inYaml = [.. yaml.Split('\n').Where(l => l.Contains("{teacher}", StringComparison.Ordinal)).Select(l => KeyOf().Match(l).Groups[1].Value)];

        inYaml.ShouldBe(GuardianTemplates, ignoreOrder: true);
    }

    [Theory]
    [MemberData(nameof(Templates))]
    public async Task Render_LongestNameAndReason_FitsAppointmentLimitAndNoticeIsCreated(string key)
    {
        string template = await TemplateAsync(key);
        string teacher = new('Ş', TeacherNameMax);
        string reason = new('ğ', Appointment.ReasonMax);

        string text = AppointmentNoticeText.Render(template, teacher, TurkeyStart, reason);

        text.ShouldContain(teacher);
        text.ShouldContain("28.12.2026 23:45");
        if (template.Contains("{reason}", StringComparison.Ordinal))
        {
            text.ShouldContain(reason);
        }

        text.Length.ShouldBeLessThanOrEqualTo(GuardianNotice.AppointmentTextMax);
        Should.NotThrow(() => GuardianNotice.ForRecord(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(TurkeyStart.DateTime),
            GuardianNoticeKind.Appointment,
            Guid.NewGuid(),
            text,
            TurkeyStart.ToUniversalTime()));
    }

    private static async Task<string> TemplateAsync(string key)
    {
        ServiceCollection services = new();
        services.AddYamlResourceLocalization();
        await using ServiceProvider provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<ILocalizationService>().AppointmentsTextAsync(key);
    }

    [GeneratedRegex(@"^\s*([A-Za-z]+)\s*:", RegexOptions.CultureInvariant)]
    private static partial Regex KeyOf();
}
