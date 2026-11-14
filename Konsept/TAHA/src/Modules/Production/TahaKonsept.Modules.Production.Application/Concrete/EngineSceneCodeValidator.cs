using TahaKonsept.Modules.Production.Application.Base;

namespace TahaKonsept.Modules.Production.Application.Concrete;

/// <summary>
/// Yazım kılavuzlarındaki iskeletlere göre motor işaretleri: HyperFrames (<c>data-composition-id</c>, <c>__timelines</c>),
/// Remotion (<c>export default</c>, <c>remotion</c>), Revideo (<c>makeScene2D</c>). Asıl derinlemesine denetim ajandaki lint'tedir.
/// </summary>
public sealed class EngineSceneCodeValidator : SceneCodeValidatorBase
{
    /// <summary>Motor işaretleri.</summary>
    /// <param name="engineKey">Motor.</param>
    /// <returns>İşaretler.</returns>
    protected override IReadOnlyList<string>? RequiredMarkers(string engineKey) => engineKey switch
    {
        "hyperframes" => ["data-composition-id", "__timelines"],
        "remotion" => ["export default", "remotion"],
        "revideo" => ["makeScene2D"],
        _ => null,
    };
}
