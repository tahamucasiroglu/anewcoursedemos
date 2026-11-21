using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Localization.Abstraction;
using NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection;
using OkulYonetim.Application.Features.Academics;
using OkulYonetim.Application.Features.Academics.Assignments;
using OkulYonetim.Domain.Academics;
using OkulYonetim.Domain.Communication;
using Shouldly;

namespace OkulYonetim.Tests.Unit.Academics;

/// <summary>
/// Spec 0013 review round 1, R1-2: yeni ödev bildirimi metni gerçek YAML şablonundan tek geçişte doldurulur. Ders adında ya da
/// başlıkta yazan <c>{title}</c>/<c>{subject}</c>/<c>{due}</c> olduğu gibi kalır (yeniden genişlemez); en uzun ders adı ve başlık
/// yer tutucularla dolu olsa bile metin okul bildiriminin sınırını (<see cref="GuardianNotice.TextMax"/>) aşmaz — art arda
/// <c>Replace</c> metni katlayıp bildirim oluşturmayı (500) düşürürdü.
/// </summary>
public sealed class AssignmentNoticeTextTests
{
    private static readonly DateOnly Due = new(2026, 11, 25);

    [Fact]
    public async Task Render_FromYaml_MatchesRequirement2()
    {
        string template = await TemplateAsync();

        AssignmentNoticeText.Render(template, "Matematik", "Kesirler alıştırması", Due)
            .ShouldBe("Yeni ödev — Matematik: Kesirler alıştırması. Teslim: 25.11.2026.");
    }

    [Fact]
    public async Task Render_PlaceholdersInsideSubjectAndTitle_AreNotExpandedAgain()
    {
        string template = await TemplateAsync();

        AssignmentNoticeText.Render(template, "Ders {title}", "Başlık {subject} {due}", Due)
            .ShouldBe("Yeni ödev — Ders {title}: Başlık {subject} {due}. Teslim: 25.11.2026.");
    }

    [Fact]
    public async Task Render_LongestFieldsFullOfPlaceholders_StaysWithinNoticeLimit()
    {
        string template = await TemplateAsync();
        string subject = Fill("{title}", Assignment.SubjectMax);
        string title = Fill("{title}", Assignment.TitleMax);

        string text = AssignmentNoticeText.Render(template, subject, title, Due);

        text.Length.ShouldBeLessThanOrEqualTo(GuardianNotice.TextMax);
        text.ShouldContain(subject);
        text.ShouldContain(title);
    }

    private static string Fill(string piece, int max) =>
        string.Concat(Enumerable.Repeat(piece, max / piece.Length));

    private static async Task<string> TemplateAsync()
    {
        ServiceCollection services = new();
        services.AddYamlResourceLocalization();
        await using ServiceProvider provider = services.BuildServiceProvider();
        return await provider.GetRequiredService<ILocalizationService>().AcademicsTextAsync(AcademicsMessages.NewAssignmentNotice);
    }
}
