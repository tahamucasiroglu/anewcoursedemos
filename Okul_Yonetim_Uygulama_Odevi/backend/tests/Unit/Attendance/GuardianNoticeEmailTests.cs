using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection;
using OkulYonetim.Application.Features.Communication;
using OkulYonetim.Application.Features.Students;
using OkulYonetim.Application.Ports;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Attendance;

/// <summary>
/// Spec 0010 AC-7, AC-8, AC-12 (gizlilik): bildirim ve e-posta metni uygulamanın gerçek YAML şablonlarından kurulur — metin
/// gereksinim 6/7'deki gibidir (gün gün.ay.yıl), gövdede yalnızca öğrenci adı, gün, okul adı ve durum vardır (velinin adı ve
/// e-postası gövdede yok, yalnızca alıcı alanında). Adda geçen süslü parantez başka yer tutucuya dönüşmez.
/// </summary>
public sealed class GuardianNoticeEmailTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);
    private static readonly GuardianContact Guardian = new(Guid.NewGuid(), Guid.NewGuid(), "Zeynep Veli", "zeynep@ornek.test");

    [Fact]
    public async Task AbsentText_FromYaml_MatchesRequirement6()
    {
        string template = await TextAsync(CommunicationMessages.AbsentText);

        GuardianNoticeEmail.Render(template, "Ali Yılmaz", Day, "Deneme Koleji")
            .ShouldBe("Ali Yılmaz bugün (20.11.2026) Deneme Koleji'nda yoklamada gelmedi olarak işaretlendi. Bilginiz yoksa okulla iletişime geçin.");
    }

    [Fact]
    public async Task CorrectedText_FromYaml_MatchesRequirement7()
    {
        string template = await TextAsync(CommunicationMessages.CorrectedText);

        GuardianNoticeEmail.Render(template, "Ali Yılmaz", Day, "Deneme Koleji").ShouldBe("Ali Yılmaz için bugünkü yoklama düzeltildi: okulda.");
    }

    [Fact]
    public async Task Build_BodyHoldsOnlyStudentDaySchoolAndStatus_RecipientOnlyInHeader()
    {
        string text = GuardianNoticeEmail.Render(await TextAsync(CommunicationMessages.AbsentText), "Ali Yılmaz", Day, "Deneme Koleji");
        string subject = GuardianNoticeEmail.Render(await TextAsync(CommunicationMessages.AbsentSubject), "Ali Yılmaz", Day, "Deneme Koleji");

        EmailMessage email = GuardianNoticeEmail.Build(Guardian, subject, text, await TextAsync(CommunicationMessages.EmailFooter));

        email.ToAddress.ShouldBe("zeynep@ornek.test");
        email.ToName.ShouldBe("Zeynep Veli");
        email.UserId.ShouldBe(Guardian.UserId);
        email.Subject.ShouldBe("Deneme Koleji — devamsızlık bildirimi");
        email.Body.ShouldStartWith(text);
        email.Body.ShouldNotContain("Zeynep");
        email.Body.ShouldNotContain("@");
        email.Body.ShouldNotContain("{");
    }

    [Fact]
    public void Render_PlaceholderInsideName_IsNotExpandedAgain() =>
        GuardianNoticeEmail.Render("{student} — {school}", "Ali {school}", Day, "Okul").ShouldBe("Ali {school} — Okul");

    private static async Task<string> TextAsync(string key)
    {
        ServiceCollection services = new();
        services.AddYamlResourceLocalization();
        await using ServiceProvider provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<ILocalizationService>().CommunicationTextAsync(key);
    }
}
