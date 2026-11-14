using TahaKonsept.Modules.Production.Application.Models;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Production.Application.Abstract;

/// <summary>
/// Sahne planlayıcı (deterministik): konsept sahnelerinden sahne listesi, seslendirme uzunluğuna göre süreler ve görsel dosya adları.
/// </summary>
public interface IScenePlanner
{
    /// <summary>Planı üretir.</summary>
    /// <param name="document">Konsept.</param>
    /// <param name="imagesEnabled">Görsel üretimi açık mı.</param>
    /// <returns>Sonuç.</returns>
    ScenePlanOutput Plan(ConceptDocument document, bool imagesEnabled);
}
