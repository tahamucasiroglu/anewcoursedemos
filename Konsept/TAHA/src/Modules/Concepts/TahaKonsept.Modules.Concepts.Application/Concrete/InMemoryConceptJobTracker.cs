using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Bellek içi iş izleyicisi (singleton); davranış <see cref="ConceptJobTrackerBase"/>'tedir.
/// </summary>
/// <param name="clock">Saat.</param>
public sealed class InMemoryConceptJobTracker(IClock clock) : ConceptJobTrackerBase(clock);
