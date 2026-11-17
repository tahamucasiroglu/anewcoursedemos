using Konsept.Modules.Concepts.Application.Base;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Bellek içi Yeniden üret bağlamı; davranış <see cref="ConceptRegenerationContextBase"/>'tedir.</summary>
/// <param name="time">Saat.</param>
public sealed class ConceptRegenerationContext(TimeProvider time) : ConceptRegenerationContextBase(time);
