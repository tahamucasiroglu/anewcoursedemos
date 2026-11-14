using NSubstitute;
using Shouldly;
using TahaKonsept.Modules.Concepts.Application.Validators;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Providers;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Doğrulayıcılar: adet sınırları, boş başlık/fikir/talimat, sahne sınırları, katalogda olmayan model.
/// </summary>
public sealed class ValidatorTests
{
    /// <summary>Konu önerisi adedi 1–100.</summary>
    [Fact]
    public void Suggest_count_bounds()
    {
        var v = new SuggestTopicsRequestValidator();
        v.Validate(new SuggestTopicsRequest { Count = 0 }).IsValid.ShouldBeFalse();
        v.Validate(new SuggestTopicsRequest { Count = 101 }).IsValid.ShouldBeFalse();
        v.Validate(new SuggestTopicsRequest { Count = 100, Hint = "Bizans" }).IsValid.ShouldBeTrue();
    }

    /// <summary>Başlık listesi boş olamaz, boş başlık olamaz.</summary>
    [Fact]
    public void Topics_generate_rules()
    {
        var v = new GenerateFromTopicsRequestValidator();
        v.Validate(new GenerateFromTopicsRequest()).IsValid.ShouldBeFalse();
        v.Validate(new GenerateFromTopicsRequest { Titles = ["A", " "] }).IsValid.ShouldBeFalse();
        v.Validate(new GenerateFromTopicsRequest { Titles = Enumerable.Range(0, 101).Select(i => "T" + i).ToList() }).IsValid.ShouldBeFalse();
        v.Validate(new GenerateFromTopicsRequest { Titles = ["A"] }).IsValid.ShouldBeTrue();
    }

    /// <summary>Fikir en az 5 karakter, adet 1–100.</summary>
    [Fact]
    public void Idea_rules()
    {
        var v = new GenerateFromIdeaRequestValidator();
        v.Validate(new GenerateFromIdeaRequest { Idea = "abc", Count = 1 }).IsValid.ShouldBeFalse();
        v.Validate(new GenerateFromIdeaRequest { Idea = "Şahi topları", Count = 0 }).IsValid.ShouldBeFalse();
        v.Validate(new GenerateFromIdeaRequest { Idea = "Şahi topları", Count = 5 }).IsValid.ShouldBeTrue();
    }

    /// <summary>Düzenleme: en az bir sahne; düzeltme: talimat zorunlu, belge verilirse geçerli olmalı.</summary>
    [Fact]
    public void Update_and_revise_rules()
    {
        var update = new UpdateConceptRequestValidator();
        update.Validate(new UpdateConceptRequest { Document = new ConceptDocument() }).IsValid.ShouldBeFalse();
        update.Validate(new UpdateConceptRequest { Document = new ConceptDocument { Scenes = [new ConceptScene { Narration = "x" }] } }).IsValid.ShouldBeTrue();

        var revise = new ReviseConceptRequestValidator();
        revise.Validate(new ReviseConceptRequest { Instruction = " " }).IsValid.ShouldBeFalse();
        revise.Validate(new ReviseConceptRequest { Instruction = "Kısalt", Document = new ConceptDocument() }).IsValid.ShouldBeFalse();
        revise.Validate(new ReviseConceptRequest { Instruction = "Kısalt" }).IsValid.ShouldBeTrue();
    }

    /// <summary>Ayarlar: modeller katalogda metin modeli olmalı.</summary>
    [Fact]
    public void Settings_models_must_be_text_models()
    {
        var catalog = Substitute.For<IModelCatalog>();
        catalog.Find(Arg.Any<string>()).Returns(ci => ci.Arg<string>() switch
        {
            "claude-sonnet-5-5" or "claude-haiku-5-5" => new ModelInfo { Key = ci.Arg<string>(), Capability = ModelCapability.Text },
            "gpt-image-2" => new ModelInfo { Key = "gpt-image-2", Capability = ModelCapability.Image },
            _ => null,
        });
        var v = new ConceptSettingsDtoValidator(catalog);
        v.Validate(new ConceptSettingsDto()).IsValid.ShouldBeTrue();
        v.Validate(new ConceptSettingsDto { ConceptModelKey = "gpt-image-2" }).IsValid.ShouldBeFalse();
        v.Validate(new ConceptSettingsDto { TopicModelKey = "yok" }).IsValid.ShouldBeFalse();
        v.Validate(new ConceptSettingsDto { Notes = new string('x', 2000) }).IsValid.ShouldBeFalse();
    }
}
