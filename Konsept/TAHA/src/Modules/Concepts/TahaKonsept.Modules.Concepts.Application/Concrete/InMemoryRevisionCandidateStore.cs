using TahaKonsept.BuildingBlocks.Application.Abstract;
using TahaKonsept.Modules.Concepts.Application.Base;

namespace TahaKonsept.Modules.Concepts.Application.Concrete;

/// <summary>
/// Bellek içi düzeltme adayı deposu (singleton); davranış <see cref="RevisionCandidateStoreBase"/>'tedir.
/// </summary>
/// <param name="clock">Saat.</param>
public sealed class InMemoryRevisionCandidateStore(IClock clock) : RevisionCandidateStoreBase(clock);
