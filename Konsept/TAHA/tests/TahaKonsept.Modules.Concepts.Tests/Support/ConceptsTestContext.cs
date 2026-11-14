using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.BuildingBlocks.Domain.Results;
using TahaKonsept.Modules.Concepts.Application.Concrete;
using TahaKonsept.Modules.Concepts.Application.Llm;
using TahaKonsept.Modules.Concepts.Domain.Entities;
using TahaKonsept.Modules.Concepts.Infrastructure.Persistence.Concrete;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Profiles.Application.Concrete;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Concepts;
using TahaKonsept.Shared.Contracts.Profiles;

namespace TahaKonsept.Modules.Concepts.Tests.Support;

/// <summary>
/// Test düzeneği: EF InMemory <see cref="ConceptsDbContext"/>, ilerleyen sahte saat, oturum kullanıcısı, sahte profil okuyucu
/// (Bizans profili), gerçek profil prompt kurucu, sahte LLM geçidi (<see cref="Gateway"/>), sahte bütçe/günlük ve bunlarla kurulmuş
/// tüm gerçek servisler. <see cref="SampleOutput"/> örnek LLM çıktısı, <see cref="SeedConceptAsync"/> veritabanına konsept ekler.
/// </summary>
public sealed class ConceptsTestContext : IDisposable
{
    private DateTimeOffset _now = new(2026, 11, 14, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Düzeneği kurar.</summary>
    public ConceptsTestContext()
    {
        Clock = Substitute.For<IClock>();
        Clock.UtcNow.Returns(_ => _now = _now.AddSeconds(1));
        CurrentUser = Substitute.For<ICurrentUser>();
        CurrentUser.UserId.Returns(UserId);
        CurrentUser.GetRequiredUserId().Returns(UserId);
        CurrentUser.IsAuthenticated.Returns(true);

        Profile = new ProfileDto
        {
            Id = Guid.CreateVersion7(),
            Name = "Bizans Günlükleri",
            Language = "tr",
            TargetDurationSec = 60,
            ChannelDescription = "Bizans tarihi; siyasi ve askerî dönüm noktaları.",
            TemplateKey = "tarih-bizans",
            IsActive = true,
        };
        Profiles = Substitute.For<IProfileReader>();
        Profiles.GetActiveAsync(UserId, Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<ProfileDto?>(Profile));
        Profiles.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(ci => Task.FromResult(ci.Arg<Guid>() == Profile.Id ? Profile : null));

        Budget = Substitute.For<IBudgetGuard>();
        Budget.EnsureCanStartProductionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Result.Success()));
        Activity = Substitute.For<IActivityLogger>();

        var options = new DbContextOptionsBuilder<ConceptsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        Db = new ConceptsDbContext(options, Clock);
        Concepts = new ConceptRepository(Db);
        MemoryRepository = new TopicMemoryRepository(Db);
        SettingsRepository = new ConceptSettingsRepository(Db);

        Normalizer = new TurkishTopicNormalizer();
        Memory = new TopicMemory(MemoryRepository, Normalizer);
        Prompts = new ConceptPromptFactory(new EmbeddedPromptLibrary(), new PromptTemplateRenderer());
        Contexts = new ConceptContextProvider(SettingsRepository, Profiles, new ProfilePromptBuilder());
        Generator = new ConceptGenerator(Gateway, Prompts);
        Topics = new TopicGenerator(Gateway, Prompts, Memory, Normalizer);
        Analyzer = new WordRepetitionAnalyzer();
        WordCheck = new WordRepetitionChecker(Analyzer, Gateway, Prompts);
        Tracker = new InMemoryConceptJobTracker(Clock);
        Queue = new ChannelConceptJobQueue();
        Candidates = new InMemoryRevisionCandidateStore(Clock);
    }

    /// <summary>Test kullanıcısı.</summary>
    public Guid UserId { get; } = Guid.CreateVersion7();

    /// <summary>Saat.</summary>
    public IClock Clock { get; }

    /// <summary>Oturum kullanıcısı.</summary>
    public ICurrentUser CurrentUser { get; }

    /// <summary>Aktif profil.</summary>
    public ProfileDto Profile { get; }

    /// <summary>Profil okuyucu.</summary>
    public IProfileReader Profiles { get; }

    /// <summary>Bütçe bekçisi.</summary>
    public IBudgetGuard Budget { get; }

    /// <summary>İşlem günlüğü.</summary>
    public IActivityLogger Activity { get; }

    /// <summary>Sahte LLM geçidi.</summary>
    public FakeProviderGateway Gateway { get; } = new();

    /// <summary>DbContext.</summary>
    public ConceptsDbContext Db { get; }

    /// <summary>Konsept deposu.</summary>
    public ConceptRepository Concepts { get; }

    /// <summary>Hafıza deposu.</summary>
    public TopicMemoryRepository MemoryRepository { get; }

    /// <summary>Ayar deposu.</summary>
    public ConceptSettingsRepository SettingsRepository { get; }

    /// <summary>Normalleştirici.</summary>
    public TurkishTopicNormalizer Normalizer { get; }

    /// <summary>Konu hafızası.</summary>
    public TopicMemory Memory { get; }

    /// <summary>Prompt fabrikası.</summary>
    public ConceptPromptFactory Prompts { get; }

    /// <summary>Bağlam kurucu.</summary>
    public ConceptContextProvider Contexts { get; }

    /// <summary>Konsept üreticisi.</summary>
    public ConceptGenerator Generator { get; }

    /// <summary>Konu üreticisi.</summary>
    public TopicGenerator Topics { get; }

    /// <summary>Kelime analizcisi.</summary>
    public WordRepetitionAnalyzer Analyzer { get; }

    /// <summary>Kelime denetimi.</summary>
    public WordRepetitionChecker WordCheck { get; }

    /// <summary>İş izleyicisi.</summary>
    public InMemoryConceptJobTracker Tracker { get; }

    /// <summary>İş kuyruğu.</summary>
    public ChannelConceptJobQueue Queue { get; }

    /// <summary>Aday deposu.</summary>
    public InMemoryRevisionCandidateStore Candidates { get; }

    /// <summary>İş çalıştırıcısı kurar.</summary>
    /// <returns>Çalıştırıcı.</returns>
    public ConceptJobProcessor CreateProcessor() =>
        new(Contexts, Generator, Topics, WordCheck, Memory, Concepts, Tracker, Budget, Activity, Clock, NullLogger<ConceptJobProcessor>.Instance);

    /// <summary>Konsept servisi kurar.</summary>
    /// <returns>Servis.</returns>
    public ConceptService CreateConceptService() => new(Concepts, CurrentUser, Clock, Memory, Activity);

    /// <summary>Düzeltme servisi kurar.</summary>
    /// <returns>Servis.</returns>
    public ConceptRevisionService CreateRevisionService() =>
        new(Concepts, CurrentUser, Budget, Contexts, Generator, WordCheck, Candidates, Memory, Clock, Activity);

    /// <summary>Toplu üretim servisi kurar.</summary>
    /// <returns>Servis.</returns>
    public ConceptGenerationService CreateGenerationService() => new(CurrentUser, Budget, Profiles, Queue, Tracker, Clock);

    /// <summary>Ayar servisi kurar.</summary>
    /// <returns>Servis.</returns>
    public ConceptSettingsService CreateSettingsService() => new(SettingsRepository, CurrentUser, Profiles, Contexts);

    /// <summary>Konu önerisi servisi kurar.</summary>
    /// <returns>Servis.</returns>
    public TopicSuggestionService CreateSuggestionService() => new(CurrentUser, Budget, Contexts, Memory, Topics, Activity);

    /// <summary>Örnek LLM konsept çıktısı (3 sahne, 1 geçersiz URL, # işaretli etiket, tekrar eden okunuş).</summary>
    /// <param name="title">Başlık.</param>
    /// <returns>Çıktı.</returns>
    public static ConceptLlmOutput SampleOutput(string title = "Kostantiniyye'nin son gecesi") => new()
    {
        Title = title,
        Hook = "29 Mayıs 1453 sabahı surlarda tek bir kapı açık kalmıştı.",
        Summary = "Kerkoporta kapısının hikâyesi.",
        Scenes =
        [
            new ConceptSceneLlm { Title = "Kanca", OnScreen = "29 Mayıs 1453", VisualPrompt = "Dawn over Theodosian walls", Narration = "Surlarda tek bir kapı açık kalmıştı.", DurationHintSec = 3 },
            new ConceptSceneLlm { Title = "Kuşatma", OnScreen = "53 gün", VisualPrompt = "Ottoman cannons firing", Narration = "Elli üç gün süren kuşatma sona yaklaşıyordu ve şehir yorgundu.", DurationHintSec = 0 },
            new ConceptSceneLlm { Title = "  ", OnScreen = string.Empty, VisualPrompt = string.Empty, Narration = string.Empty, DurationHintSec = 2 },
            new ConceptSceneLlm { Title = "Son", OnScreen = "Kerkoporta", VisualPrompt = "Small gate in ancient wall", Narration = "Ve tarih o küçük kapıdan girdi.", DurationHintSec = 2.5 },
        ],
        Sources =
        [
            new ConceptSourceLlm { Title = "Britannica", Url = "https://www.britannica.com/event/Fall-of-Constantinople-1453" },
            new ConceptSourceLlm { Title = "Uydurma", Url = "not-a-url" },
        ],
        Pronunciations =
        [
            new PronunciationLlm { Written = "Kerkoporta", Spoken = "Kerkoporta" },
            new PronunciationLlm { Written = "Constantine", Spoken = "Konstantin" },
            new PronunciationLlm { Written = "constantine", Spoken = "Konstantin" },
        ],
        Tags = ["#bizans", "tarih", "Bizans", " "],
    };

    /// <summary>Veritabanına konsept ekler.</summary>
    /// <param name="title">Başlık.</param>
    /// <param name="userId">Sahip (varsayılan test kullanıcısı).</param>
    /// <returns>Konsept.</returns>
    public async Task<Concept> SeedConceptAsync(string title = "Kerkoporta", Guid? userId = null)
    {
        var document = new ConceptDocument
        {
            Title = title,
            Language = "tr",
            Format = VideoFormat.Vertical,
            TargetDurationSec = 60,
            Scenes = [new ConceptScene { Title = "S1", Narration = "Bir varmış bir yokmuş." }],
        };
        var concept = Concept.Create(userId ?? UserId, Profile.Id, ConceptOrigin.Topic, document, "fake-model", 10, 5, 0.02m);
        await Concepts.AddAsync(concept);
        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();
        return concept;
    }

    /// <summary>Kaynakları bırakır.</summary>
    public void Dispose() => Db.Dispose();
}
