using NSubstitute;
using Shouldly;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Domain.Errors;
using TahaKonsept.Modules.Concepts.Tests.Support;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Profiles;
using Xunit;

namespace TahaKonsept.Modules.Concepts.Tests.Application;

/// <summary>
/// Toplu üretim başlatıcı ve iş izleyicisi: bütçe/profil kontrolü, kuyruğa alma, güncel iş seçimi, iptal.
/// </summary>
public sealed class ConceptGenerationServiceTests : IDisposable
{
    private readonly ConceptsTestContext _ctx = new();

    /// <summary>Başarılı başlatma işi kuyruğa alır ve Queued döner; başlıklar tekrarsızlaştırılır.</summary>
    [Fact]
    public async Task Start_enqueues_job()
    {
        var service = _ctx.CreateGenerationService();
        var result = await service.StartFromTopicsAsync(new GenerateFromTopicsRequest { Titles = ["A", " a ", "B"] }, TestContext.Current.CancellationToken);

        result.Value.Status.ShouldBe(ConceptJobStatus.Queued);
        result.Value.Total.ShouldBe(2);
        await foreach (var job in _ctx.Queue.ReadAllAsync(TestContext.Current.CancellationToken))
        {
            job.Titles.ShouldBe(["A", "B"]);
            job.ProfileId.ShouldBe(_ctx.Profile.Id);
            break;
        }
    }

    /// <summary>Bütçe aşımında iş başlamaz; profil yoksa NoProfile.</summary>
    [Fact]
    public async Task Budget_and_profile_guards()
    {
        var service = _ctx.CreateGenerationService();
        var budgetError = Error.Conflict("providers.budget_exceeded", "Aylık bütçe aşıldı.");
        _ctx.Budget.EnsureCanStartProductionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Failure(budgetError)));
        (await service.StartFromIdeaAsync(new GenerateFromIdeaRequest { Idea = "Bir fikir", Count = 3 }, TestContext.Current.CancellationToken)).Error.ShouldBe(budgetError);
        service.GetCurrent().Value.Status.ShouldBe(ConceptJobStatus.Idle);

        _ctx.Budget.EnsureCanStartProductionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        _ctx.Profiles.GetActiveAsync(_ctx.UserId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<ProfileDto?>(null));
        (await service.StartFromIdeaAsync(new GenerateFromIdeaRequest { Idea = "Bir fikir", Count = 3 }, TestContext.Current.CancellationToken)).Error.ShouldBe(ConceptErrors.NoProfile);
    }

    /// <summary>Güncel iş: çalışan önce; sıradakiler sayılır; iptal sıradakileri hemen Cancelled yapar.</summary>
    [Fact]
    public async Task Tracker_current_and_cancel()
    {
        var service = _ctx.CreateGenerationService();
        var first = (await service.StartFromTopicsAsync(new GenerateFromTopicsRequest { Titles = ["A"] }, TestContext.Current.CancellationToken)).Value;
        await service.StartFromTopicsAsync(new GenerateFromTopicsRequest { Titles = ["B"] }, TestContext.Current.CancellationToken);
        _ctx.Tracker.Update(first.Id, d => d with { Status = ConceptJobStatus.Running });

        var current = service.GetCurrent().Value;
        current.Id.ShouldBe(first.Id);
        current.QueuedJobs.ShouldBe(1);

        service.CancelCurrent();
        _ctx.Tracker.IsCancelled(first.Id).ShouldBeTrue();
        _ctx.Tracker.GetCancellationToken(first.Id).IsCancellationRequested.ShouldBeTrue();
        service.GetCurrent().Value.QueuedJobs.ShouldBe(0);
    }

    /// <summary>Ayarlar: kayıt yokken varsayılanlar ve tarih profilinde otomatik gerçek bilgi; kaydedince geri okunur.</summary>
    [Fact]
    public async Task Settings_defaults_and_save()
    {
        var settings = _ctx.CreateSettingsService();
        var defaults = (await settings.GetAsync(TestContext.Current.CancellationToken)).Value;
        defaults.ConceptModelKey.ShouldBe("claude-sonnet-5-5");
        defaults.TopicModelKey.ShouldBe("claude-haiku-5-5");
        defaults.FactGrounding.ShouldBeNull();
        defaults.EffectiveFactGrounding.ShouldBeTrue();

        await settings.SaveAsync(defaults with { Tone = "  Dramatik ", FactGrounding = false, WordCheckModelKey = " " }, TestContext.Current.CancellationToken);
        var saved = (await settings.GetAsync(TestContext.Current.CancellationToken)).Value;
        saved.Tone.ShouldBe("Dramatik");
        saved.EffectiveFactGrounding.ShouldBeFalse();
        saved.WordCheckModelKey.ShouldBe("claude-haiku-5-5");
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => _ctx.Dispose();
}
