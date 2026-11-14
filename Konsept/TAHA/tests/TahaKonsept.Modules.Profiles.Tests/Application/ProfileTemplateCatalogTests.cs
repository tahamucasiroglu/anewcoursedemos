using Shouldly;
using TahaKonsept.Modules.Profiles.Application.Concrete;
using TahaKonsept.Modules.Profiles.Application.Validation;
using TahaKonsept.Shared.Contracts.Profiles;
using Xunit;

namespace TahaKonsept.Modules.Profiles.Tests.Application;

/// <summary>
/// Gömülü şablon kataloğu testleri: 8 şablon, benzersiz anahtarlar, dolu alanlar, doğrulayıcıdan geçme, anahtarla bulma.
/// </summary>
public sealed class ProfileTemplateCatalogTests
{
    private readonly EmbeddedProfileTemplateCatalog _catalog = new();

    /// <summary>Beklenen şablonlar yüklenir.</summary>
    [Fact]
    public void Loads_all_builtin_templates()
    {
        var all = _catalog.GetAll();

        all.Select(t => t.Title).ShouldBe(
        [
            "Tarih – Bizans", "Tarih – Osmanlı", "Tarih – Selçuklu", "Teknoloji – Antik Teknoloji",
            "Teknoloji – Günümüz", "Bilim – Uzay", "Spor – Futbol Tarihi", "Gizem – Çözülmemiş Olaylar",
        ]);
        all.Select(t => t.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(all.Count);
    }

    /// <summary>Her şablon doldurulmuş ve geçerli bir profil isteğine dönüşür.</summary>
    [Fact]
    public void Every_template_is_a_valid_profile_request()
    {
        var validator = new SaveProfileRequestValidator();
        foreach (var t in _catalog.GetAll())
        {
            t.Name.ShouldNotBeNullOrWhiteSpace();
            t.ChannelDescription.Length.ShouldBeGreaterThan(150, t.Key);
            var request = new SaveProfileRequest
            {
                Name = t.Name,
                Language = t.Language,
                Format = t.Format,
                TargetDurationSec = t.TargetDurationSec,
                ChannelDescription = t.ChannelDescription,
                DefaultTone = t.DefaultTone,
                DefaultAudience = t.DefaultAudience,
                DefaultNarrativeStyle = t.DefaultNarrativeStyle,
                TemplateKey = t.Key,
            };
            validator.Validate(request).IsValid.ShouldBeTrue(t.Key);
        }
    }

    /// <summary>Anahtarla bulma büyük/küçük harf duyarsızdır.</summary>
    [Fact]
    public void Find_is_case_insensitive()
    {
        _catalog.Find("TARIH-BIZANS")!.Name.ShouldBe("Bizans Günlükleri");
        _catalog.Find("yok").ShouldBeNull();
        _catalog.Find(" ").ShouldBeNull();
    }
}
