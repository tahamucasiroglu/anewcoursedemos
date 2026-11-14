using System.Globalization;
using TahaKonsept.Modules.Production.Application.Abstract;
using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Application.Base;

/// <summary>
/// <see cref="IScenePlanner"/> ortak davranışı: konsept sahnelerini indeks sırasıyla <see cref="PlannedScene"/>'e çevirir; süreyi
/// <see cref="EstimateDurationSec"/> (somut sınıf) ile seslendirmeden tahmin eder ve süre ipucuyla büyüğünü alır; görsel açıksa ve istem varsa
/// sahneye <c>scene-{i}-1.png</c> adlı tek görsel bağlar. Ses ayarları (hız, model) plana girmez — böylece sesi değiştirmek sahne kodunu bozmaz.
/// </summary>
public abstract class ScenePlannerBase : IScenePlanner
{
    /// <summary>Asgari sahne süresi (sn).</summary>
    public const double MinSceneSec = 2.5;

    /// <summary>Azami sahne süresi (sn).</summary>
    public const double MaxSceneSec = 180;

    /// <summary>Planı üretir.</summary>
    /// <param name="document">Konsept.</param>
    /// <param name="imagesEnabled">Görsel üretimi açık mı.</param>
    /// <returns>Plan.</returns>
    public ScenePlanOutput Plan(ConceptDocument document, bool imagesEnabled)
    {
        ArgumentNullException.ThrowIfNull(document);
        var scenes = document.Scenes
            .OrderBy(s => s.Index)
            .Select((scene, i) => new PlannedScene
            {
                Index = i,
                Title = scene.Title?.Trim() ?? string.Empty,
                OnScreen = scene.OnScreen?.Trim() ?? string.Empty,
                Narration = scene.Narration?.Trim() ?? string.Empty,
                VisualPrompt = scene.VisualPrompt?.Trim() ?? string.Empty,
                DurationSec = Math.Round(Math.Clamp(Math.Max(EstimateDurationSec(scene.Narration ?? string.Empty), scene.DurationHintSec ?? 0), MinSceneSec, MaxSceneSec), 2),
                ImageNames = imagesEnabled && !string.IsNullOrWhiteSpace(scene.VisualPrompt)
                    ? ["scene-" + i.ToString(CultureInfo.InvariantCulture) + "-1.png"]
                    : [],
            })
            .ToList();
        return new ScenePlanOutput { Scenes = scenes, TotalDurationSec = Math.Round(scenes.Sum(s => s.DurationSec), 2) };
    }

    /// <summary>Seslendirme metninden süre tahmini (sn).</summary>
    /// <param name="narration">Seslendirme.</param>
    /// <returns>Süre.</returns>
    protected abstract double EstimateDurationSec(string narration);
}
