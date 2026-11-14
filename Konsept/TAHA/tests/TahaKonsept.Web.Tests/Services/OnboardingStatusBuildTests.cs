using Shouldly;
using TahaKonsept.Web.Models;
using TahaKonsept.Web.Services.Onboarding.Base;
using Xunit;

namespace TahaKonsept.Web.Tests.Services;

/// <summary>
/// <see cref="OnboardingServiceBase.Build"/> testleri: adım sırası, Done/Waiting/Unknown kuralları, kalan sayısı, tamamlanma.
/// </summary>
public sealed class OnboardingStatusBuildTests
{
    /// <summary>Hiçbir şey yokken beş adım da bekler.</summary>
    [Fact]
    public void Fresh_install_has_five_waiting_steps_in_order()
    {
        var status = OnboardingServiceBase.Build(new OnboardingFacts
        {
            LlmKeyPlatforms = [],
            PairedAgents = 0,
            OnlineAgents = 0,
            Profiles = 0,
            Concepts = 0,
            Videos = 0,
        });

        status.Steps.Select(s => s.Key).ShouldBe(OnboardingStepKeys.All);
        status.Steps.ShouldAllBe(s => s.State == OnboardingStepState.Waiting);
        status.Remaining.ShouldBe(5);
        status.IsComplete.ShouldBeFalse();
    }

    /// <summary>Her şey varken tamamdır ve rozet sıfırdır.</summary>
    [Fact]
    public void Everything_present_is_complete()
    {
        var status = OnboardingServiceBase.Build(new OnboardingFacts
        {
            LlmKeyPlatforms = ["Anthropic (Claude)"],
            PairedAgents = 1,
            OnlineAgents = 1,
            Profiles = 2,
            Concepts = 3,
            Videos = 1,
        });

        status.IsComplete.ShouldBeTrue();
        status.Remaining.ShouldBe(0);
        status.Steps[0].Detail.ShouldBe("Anthropic (Claude) kayıtlı.");
        status.Steps[1].Detail.ShouldBe("1 bilgisayar eşli · 1 çevrimiçi.");
        status.Steps[2].Detail.ShouldBe("2 profil.");
    }

    /// <summary>Eşli ama çevrimdışı bilgisayar adımı tamamlar; açıklama bağlı olmadığını söyler.</summary>
    [Fact]
    public void Paired_but_offline_agent_counts_as_done()
    {
        var status = OnboardingServiceBase.Build(new OnboardingFacts { PairedAgents = 1, OnlineAgents = 0 });

        var agent = status.Steps.Single(s => s.Key == OnboardingStepKeys.Agent);
        agent.State.ShouldBe(OnboardingStepState.Done);
        agent.Detail.ShouldContain("bağlı değil");
    }

    /// <summary>Alınamayan bilgi Unknown olur; ne kalana ne tamamlanana sayılır.</summary>
    [Fact]
    public void Missing_facts_are_unknown_and_not_counted_as_remaining()
    {
        var status = OnboardingServiceBase.Build(new OnboardingFacts { LlmKeyPlatforms = ["OpenAI"], Profiles = 0 });

        status.Steps.Count(s => s.State == OnboardingStepState.Unknown).ShouldBe(3);
        status.Remaining.ShouldBe(1);
        status.IsComplete.ShouldBeFalse();
    }
}
