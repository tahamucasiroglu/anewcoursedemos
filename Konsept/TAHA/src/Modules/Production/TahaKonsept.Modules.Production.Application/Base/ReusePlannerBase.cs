using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Concrete;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Modules.Production.Domain.Entities;
using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IReusePlanner"/> ortak davranışı: <see cref="SimulatedStageOutputView"/> üzerinde aşamaları hat sırasıyla (sahne bazlılarda sahne sahne)
/// işler. Yeni girdi hash'i kaynak işin biten çalıştırmasıyla aynıysa yeniden kullanılır (kaynağın çıktı hash'i görünüme yazılır); değilse aşama
/// yeniden çalışır — deterministikse çıktı gerçekten hesaplanır (sonraki aşamalar boşuna kirlenmesin), değilse yapay hash yazılır.
/// </summary>
public abstract class ReusePlannerBase : IReusePlanner
{
    private readonly IStageHandlerRegistry _handlers;
    private readonly IStageInputHasher _hasher;
    private readonly IScenePlanner _planner;

    /// <summary>Oluşturur.</summary>
    /// <param name="handlers">Aşama işleyicileri.</param>
    /// <param name="hasher">Hash'leyici.</param>
    /// <param name="planner">Sahne planlayıcı.</param>
    protected ReusePlannerBase(IStageHandlerRegistry handlers, IStageInputHasher hasher, IScenePlanner planner)
    {
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    /// <summary>Planı çıkarır.</summary>
    /// <param name="context">Yeni işin bağlamı.</param>
    /// <param name="baseRuns">Kaynak işin çalıştırmaları.</param>
    /// <param name="cancellationToken">İptal belirteci.</param>
    /// <returns>Plan.</returns>
    public async Task<ReusePlan> PlanAsync(PipelineContext context, IReadOnlyList<StageRun> baseRuns, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(baseRuns);
        var view = new SimulatedStageOutputView(() => _planner.Plan(context.Document, context.Settings.ImageMode != MediaMode.Off));
        var ctx = context with { Outputs = view };
        var decisions = new List<StageDecision>();
        foreach (var handler in _handlers.All)
        {
            var scenes = handler.PerScene
                ? Enumerable.Range(0, view.ScenePlan.Scenes.Count).Select(i => (int?)i).ToList()
                : [null];
            foreach (var scene in scenes)
            {
                var hash = _hasher.HashInput(handler.Stage, scene, handler.BuildHashInput(ctx, scene));
                var match = baseRuns.FirstOrDefault(r =>
                    r.Stage == handler.Stage && r.SceneIndex == scene && r.IsDone && r.InputHash == hash && !string.IsNullOrEmpty(r.OutputHash));
                if (match is not null)
                {
                    view.Set(handler.Stage, scene, match.OutputHash!, match.OutputJson, match.ArtifactsJson);
                    decisions.Add(new StageDecision { Stage = handler.Stage, SceneIndex = scene, InputHash = hash, Reuse = true, BaseRunId = match.Id });
                    continue;
                }

                string? json = null;
                var outputHash = "dirty:" + hash;
                if (handler.IsDeterministic(ctx))
                {
                    var request = new StageExecutionRequest { SceneIndex = scene, InputHash = hash };
                    var executed = await handler.ExecuteAsync(ctx, request, cancellationToken);
                    if (executed.IsSuccess)
                    {
                        json = executed.Value.OutputJson;
                        outputHash = _hasher.HashOutput(hash, json);
                    }
                }

                view.Set(handler.Stage, scene, outputHash, json, null);
                decisions.Add(new StageDecision { Stage = handler.Stage, SceneIndex = scene, InputHash = hash, Reuse = false });
            }
        }

        return new ReusePlan { Decisions = decisions };
    }
}
