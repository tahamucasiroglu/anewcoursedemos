using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Abstract;
using TahaKonsept.Modules.Agents.Application.Models;
using TahaKonsept.Modules.Diagnostics.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Common;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Modules.Production.Infrastructure.DependencyInjection;
using TahaKonsept.Modules.Production.Infrastructure.Persistence.Concrete;
using TahaKonsept.Modules.Profiles.Application.Abstract;
using TahaKonsept.Modules.Providers.Application.Abstract;
using TahaKonsept.Shared.Contracts.Agent;
using TahaKonsept.Shared.Contracts.Agent.Payloads;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Tests.Support;

/// <summary>
/// Production test düzeneği: gerçek Application/Infrastructure kayıtları (<c>AddProductionCore</c>) + EF InMemory + sahte geçitler
/// (LLM, online medya, ajan, artefakt, bütçe, konsept, işlem günlüğü). Orkestratör turu (<see cref="TickAsync"/>), sahte ajanın komutları
/// tamamlaması/başarısız etmesi (<see cref="CompletePendingAsync"/>, <see cref="FailNextAsync"/>) ve sıra bitene kadar koşturma
/// (<see cref="RunUntilIdleAsync"/>) sağlar. Her işlem yeni DI kapsamında (gerçekteki gibi ayrı DbContext).
/// </summary>
public sealed class ProductionTestHarness : IAsyncDisposable
{
    private readonly ServiceProvider _root;

    /// <summary>Düzeneği kurar.</summary>
    public ProductionTestHarness()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var database = "production-" + Guid.NewGuid().ToString("N");
        services.AddDbContext<ProductionDbContext>(o => o.UseInMemoryDatabase(database));
        services.AddProductionCore();
        services.AddSingleton<IClock>(Clock);
        services.AddSingleton<ICurrentUser>(new TestCurrentUser(UserId));
        services.AddSingleton<IProviderGateway>(Provider);
        services.AddSingleton<IMediaGateway>(Media);
        services.AddSingleton<IAgentGateway>(Agent);
        services.AddSingleton<IArtifactStore>(Artifacts);
        services.AddSingleton<IBudgetGuard>(Budget);
        services.AddSingleton<IConceptSource>(Concepts);
        services.AddSingleton<IActivityLogger>(Activity);
        services.AddSingleton(Substitute.For<IProfileReader>());
        services.AddSingleton(Catalog);
        _root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    /// <summary>Test kullanıcısı.</summary>
    public Guid UserId { get; } = Guid.CreateVersion7();

    /// <summary>Saat.</summary>
    public FakeClock Clock { get; } = new();

    /// <summary>LLM geçidi.</summary>
    public FakeProviderGateway Provider { get; } = new();

    /// <summary>Online medya geçidi.</summary>
    public FakeMediaGateway Media { get; } = new();

    /// <summary>Ajan geçidi.</summary>
    public FakeAgentGateway Agent { get; } = new();

    /// <summary>Artefakt deposu.</summary>
    public FakeArtifactStore Artifacts { get; } = new();

    /// <summary>Bütçe bekçisi.</summary>
    public FakeBudgetGuard Budget { get; } = new();

    /// <summary>Konsept portu.</summary>
    public FakeConceptSource Concepts { get; } = new();

    /// <summary>İşlem günlüğü.</summary>
    public RecordingActivityLogger Activity { get; } = new();

    /// <summary>Model kataloğu (varsayılan: hiçbir model bulunmaz → yerel/online varsayılan hız).</summary>
    public IModelCatalog Catalog { get; } = Substitute.For<IModelCatalog>();

    /// <summary>Yeni kapsamda çalıştırır.</summary>
    /// <typeparam name="T">Sonuç türü.</typeparam>
    /// <param name="action">İşlem.</param>
    /// <returns>Sonuç.</returns>
    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var scope = _root.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>Yeni kapsamda servis çağırır.</summary>
    /// <typeparam name="TService">Servis.</typeparam>
    /// <typeparam name="T">Sonuç.</typeparam>
    /// <param name="action">İşlem.</param>
    /// <returns>Sonuç.</returns>
    public Task<T> WithAsync<TService, T>(Func<TService, Task<T>> action)
        where TService : notnull =>
        InScopeAsync(sp => action(sp.GetRequiredService<TService>()));

    /// <summary>Ayarları kaydeder.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <returns>Görev.</returns>
    public Task SaveSettingsAsync(ProductionSettingsDto settings) =>
        WithAsync<IProductionSettingsService, bool>(async s => (await s.UpdateAsync(settings, CancellationToken.None)).IsSuccess);

    /// <summary>Örnek konsepti ekleyip sıraya koyar.</summary>
    /// <param name="title">Başlık.</param>
    /// <returns>Konsept kimliği.</returns>
    public async Task<Guid> EnqueueSampleAsync(string title = "İstanbul'un Fethi")
    {
        var conceptId = Concepts.Add(FakeConceptSource.Sample(title));
        var added = await WithAsync<IProductionQueueService, bool>(async s => (await s.AddAsync(new AddToQueueRequest { ConceptIds = [conceptId] }, CancellationToken.None)).IsSuccess);
        if (!added)
        {
            throw new InvalidOperationException("Sıraya eklenemedi");
        }

        return conceptId;
    }

    /// <summary>Sıra durumu.</summary>
    /// <returns>Durum.</returns>
    public Task<QueueStateDto> StateAsync() =>
        WithAsync<IProductionQueueService, QueueStateDto>(async s => (await s.GetStateAsync(CancellationToken.None)).Value);

    /// <summary>Orkestratör turu.</summary>
    /// <returns>Görev.</returns>
    public Task TickAsync() => WithAsync<IProductionOrchestrator, int>(o => o.TickAsync(CancellationToken.None));

    /// <summary>Bekleyen tüm ajan komutlarını başarıyla tamamlar.</summary>
    /// <returns>Tamamlanan komut sayısı.</returns>
    public async Task<int> CompletePendingAsync()
    {
        var count = 0;
        while (Agent.Pending.TryDequeue(out var command))
        {
            var report = SuccessReport(command);
            await WithAsync<IAgentStageEventHandler, bool>(async h =>
            {
                await h.OnStageCompletedAsync(AgentContext(), report, CancellationToken.None);
                return true;
            });
            count++;
        }

        return count;
    }

    /// <summary>Sıradaki ajan komutunu başarısız eder.</summary>
    /// <param name="errorCode">Hata kodu.</param>
    /// <param name="retryable">Yeniden denenebilir mi.</param>
    /// <param name="details">Ayrıntı.</param>
    /// <returns>Başarısız edilen komut.</returns>
    public async Task<StageCommand> FailNextAsync(string errorCode, bool retryable = false, string? details = null)
    {
        if (!Agent.Pending.TryDequeue(out var command))
        {
            throw new InvalidOperationException("Bekleyen ajan komutu yok");
        }

        var report = new StageFailureReport
        {
            CommandId = command.CommandId,
            JobId = command.JobId,
            Stage = command.Stage,
            SceneIndex = command.SceneIndex,
            ErrorCode = errorCode,
            Message = "Sahte hata: " + errorCode,
            Details = details,
            Retryable = retryable,
        };
        await WithAsync<IAgentStageEventHandler, bool>(async h =>
        {
            await h.OnStageFailedAsync(AgentContext(), report, CancellationToken.None);
            return true;
        });
        return command;
    }

    /// <summary>Tur + ajan tamamlamayı sıra boşalana (ya da sınır) kadar tekrarlar.</summary>
    /// <param name="maxRounds">Azami tur.</param>
    /// <returns>Görev.</returns>
    public async Task RunUntilIdleAsync(int maxRounds = 200)
    {
        for (var i = 0; i < maxRounds; i++)
        {
            await TickAsync();
            var completed = await CompletePendingAsync();
            if (completed == 0)
            {
                var state = await StateAsync();
                if (state.Status == QueueRunState.Idle && state.Items.All(x => x.Status is not (QueueItemStatus.Running or QueueItemStatus.Paused)))
                {
                    return;
                }

                if (!Agent.Online || state.Status == QueueRunState.Paused)
                {
                    return;
                }
            }
        }

        throw new InvalidOperationException("Sıra bitmedi");
    }

    /// <summary>Tüm işler (çalıştırmalarla, takipsiz).</summary>
    /// <returns>İşler (eskiden yeniye).</returns>
    public Task<List<Job>> JobsAsync() =>
        InScopeAsync(async sp => await sp.GetRequiredService<ProductionDbContext>().Jobs.AsNoTracking().Include(j => j.Runs).OrderBy(j => j.CreatedAtUtc).ToListAsync());

    /// <summary>Tüm videolar.</summary>
    /// <returns>Videolar (eskiden yeniye).</returns>
    public Task<List<Video>> VideosAsync() =>
        InScopeAsync(async sp => await sp.GetRequiredService<ProductionDbContext>().Videos.AsNoTracking().OrderBy(v => v.CreatedAtUtc).ToListAsync());

    /// <summary>Kaynakları bırakır.</summary>
    /// <returns>Görev.</returns>
    public ValueTask DisposeAsync() => _root.DisposeAsync();

    private AgentCallContext AgentContext() => new("test-connection", UserId, null);

    private static StageResultReport SuccessReport(StageCommand command)
    {
        string? output = null;
        if (command.Stage == PipelineStage.Speech)
        {
            var payload = ProductionJson.Read<SpeechStagePayload>(command.PayloadJson)!;
            var timings = payload.Segments.Select((s, i) => new SpeechSceneTiming { SceneIndex = s.SceneIndex, StartSec = i * 3.0, DurationSec = 3.0, FileName = AgentArtifactNames.SceneAudio(s.SceneIndex) }).ToList();
            output = ProductionJson.Write(new SpeechStageResult { TotalDurationSec = timings.Count * 3.0, Scenes = timings });
        }
        else if (command.Stage == PipelineStage.Align)
        {
            output = ProductionJson.Write(new AlignStageResult { Words = [new CaptionWordItem { Text = "Merhaba", StartMs = 0, EndMs = 400 }] });
        }

        return new StageResultReport
        {
            CommandId = command.CommandId,
            JobId = command.JobId,
            Stage = command.Stage,
            SceneIndex = command.SceneIndex,
            OutputJson = output,
            DurationMs = 1200,
        };
    }
}
