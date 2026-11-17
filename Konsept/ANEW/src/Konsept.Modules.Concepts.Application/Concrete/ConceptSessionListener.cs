using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Application.Base;

namespace Konsept.Modules.Concepts.Application.Concrete;

/// <summary>Çıkış ve banda süren konsept/öneri partisini durdurur (spec 0014 Req 20, spec 0024 Req 4).</summary>
/// <param name="registry">İş kaydı.</param>
public sealed class ConceptSessionListener(IConceptJobRegistry registry) : ConceptSessionListenerBase(registry);
