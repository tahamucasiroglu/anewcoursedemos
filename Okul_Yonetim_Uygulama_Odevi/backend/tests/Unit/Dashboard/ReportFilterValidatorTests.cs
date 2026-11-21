using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection;
using OkulYonetim.Application.Features.Dashboard.Export;
using OkulYonetim.Application.Features.Dashboard.Reports;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Dashboard;

/// <summary>
/// Spec 0017 AC-6 (sunucu tarafı): başlangıç ya da bitiş eksik, başlangıç &gt; bitiş ve uçlar dahil 367 günlük aralık Türkçe alan hatası
/// verir; 366 gün ve tek gün kabul; eşik 0–366; sayfa 1 ya da büyük; borç raporu tarihsizdir; dışa aktarma aynı kuralları rapor türüne göre
/// uygular. Metinler gerçek <c>dashboard.tr.yaml</c>'dan.
/// </summary>
public sealed class ReportFilterValidatorTests : IAsyncDisposable
{
    private static readonly DateOnly Nov1 = new(2026, 11, 1);
    private readonly ServiceProvider provider;
    private readonly ILocalizationService localization;

    public ReportFilterValidatorTests()
    {
        ServiceCollection services = new();
        services.AddYamlResourceLocalization();
        this.provider = services.BuildServiceProvider();
        this.localization = this.provider.GetRequiredService<ILocalizationService>();
    }

    public ValueTask DisposeAsync() => this.provider.DisposeAsync();

    [Fact]
    public void Attendance_MissingDates_FieldErrors()
    {
        ValidationResult result = this.Attendance(new GetAttendanceReportQuery(null, null, null, null, null));
        Messages(result, "StartDate").ShouldBe(["Başlangıç tarihi zorunludur"]);
        Messages(result, "EndDate").ShouldBe(["Bitiş tarihi zorunludur"]);
    }

    [Fact]
    public void Attendance_FromAfterTo_FieldError()
    {
        ValidationResult result = this.Attendance(new GetAttendanceReportQuery(Nov1.AddDays(1), Nov1, null, null, null));
        Messages(result, "EndDate").ShouldBe(["Başlangıç tarihi bitiş tarihinden sonra olamaz"]);
    }

    [Theory]
    [InlineData(366, true)]
    [InlineData(367, false)]
    [InlineData(1, true)]
    public void Payments_RangeLength_366DaysInclusiveIsTheLimit(int days, bool valid)
    {
        GetPaymentsReportQueryValidator validator = new(this.localization);
        ValidationResult result = validator.Validate(new GetPaymentsReportQuery(Nov1, Nov1.AddDays(days - 1), null));
        result.IsValid.ShouldBe(valid);
        if (!valid)
        {
            Messages(result, "EndDate").ShouldBe(["Tarih aralığı en fazla 366 gün olabilir"]);
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(366, true)]
    [InlineData(367, false)]
    public void Attendance_MinAbsent_ZeroTo366(int minAbsent, bool valid)
    {
        ValidationResult result = this.Attendance(new GetAttendanceReportQuery(Nov1, Nov1, null, null, minAbsent));
        result.IsValid.ShouldBe(valid);
        if (!valid)
        {
            Messages(result, "MinAbsent").ShouldBe(["Eşik 0 ile 366 arasında tam sayı olmalı"]);
        }
    }

    [Fact]
    public void Page_ZeroRejected()
    {
        Messages(this.Attendance(new GetAttendanceReportQuery(Nov1, Nov1, null, null, null, 0)), "Page").ShouldBe(["Sayfa 1 ya da daha büyük olmalı"]);
        new GetBalancesReportQueryValidator(this.localization).Validate(new GetBalancesReportQuery(null, false, 0)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Balances_HasNoDateRange_Valid()
    {
        new GetBalancesReportQueryValidator(this.localization).Validate(new GetBalancesReportQuery(null, true)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Export_AppliesRangeRulesPerKind()
    {
        ExportReportCommandValidator validator = new(this.localization);
        validator.Validate(new ExportReportCommand(ReportKind.Balances, null, null, null, null, null, true)).IsValid.ShouldBeTrue();
        ValidationResult payments = validator.Validate(new ExportReportCommand(ReportKind.Payments, null, Nov1, null, null, null, false));
        Messages(payments, "StartDate").ShouldBe(["Başlangıç tarihi zorunludur"]);
        ValidationResult attendance = validator.Validate(new ExportReportCommand(ReportKind.Attendance, Nov1, Nov1.AddDays(366), null, null, null, false));
        Messages(attendance, "EndDate").ShouldBe(["Tarih aralığı en fazla 366 gün olabilir"]);
        Messages(validator.Validate(new ExportReportCommand((ReportKind)9, Nov1, Nov1, null, null, null, false)), "Kind").ShouldBe(["Rapor türü geçersiz"]);
    }

    private static string[] Messages(ValidationResult result, string property) =>
        [.. result.Errors.Where(e => e.PropertyName == property).Select(e => e.ErrorMessage)];

    private ValidationResult Attendance(GetAttendanceReportQuery query) => new GetAttendanceReportQueryValidator(this.localization).Validate(query);
}
