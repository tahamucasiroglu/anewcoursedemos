using TahaKonsept.Shared.Contracts.Production;

namespace TahaKonsept.Modules.Production.Application.Common;

/// <summary>
/// Hat aşamalarının sabit bilgisi: sıra (<see cref="Ordered"/>), sahne bazlı aşamalar (<see cref="IsPerScene"/>),
/// Türkçe adlar (<see cref="DisplayName"/>) ve bilinen video motorları (<see cref="KnownEngines"/>).
/// </summary>
public static class PipelineStages
{
    /// <summary>Aşamalar hat sırasıyla.</summary>
    public static IReadOnlyList<PipelineStage> Ordered { get; } =
    [
        PipelineStage.Script,
        PipelineStage.FactSheet,
        PipelineStage.ScenePlan,
        PipelineStage.SceneCode,
        PipelineStage.Images,
        PipelineStage.Normalize,
        PipelineStage.Speech,
        PipelineStage.Align,
        PipelineStage.SceneRender,
        PipelineStage.Compose,
        PipelineStage.Mux,
    ];

    /// <summary>Bilinen video motoru anahtarları.</summary>
    public static IReadOnlyList<string> KnownEngines { get; } = ["hyperframes", "remotion", "revideo"];

    /// <summary>Aşama sahne başına mı çalışır.</summary>
    /// <param name="stage">Aşama.</param>
    /// <returns>Sahne bazlıysa <c>true</c>.</returns>
    public static bool IsPerScene(PipelineStage stage) => stage is PipelineStage.SceneCode or PipelineStage.SceneRender;

    /// <summary>Aşamanın Türkçe adı.</summary>
    /// <param name="stage">Aşama.</param>
    /// <returns>Ad.</returns>
    public static string DisplayName(PipelineStage stage) => stage switch
    {
        PipelineStage.Script => "Konsept inceleniyor",
        PipelineStage.FactSheet => "Bilgi özeti",
        PipelineStage.ScenePlan => "Sahne planı",
        PipelineStage.SceneCode => "Sahne kodu",
        PipelineStage.Images => "Görseller",
        PipelineStage.Normalize => "Okunuş",
        PipelineStage.Speech => "Seslendirme",
        PipelineStage.Align => "Hizalama",
        PipelineStage.SceneRender => "Sahne render",
        PipelineStage.Compose => "Birleştirme",
        PipelineStage.Mux => "Son dosya",
        _ => stage.ToString(),
    };
}
