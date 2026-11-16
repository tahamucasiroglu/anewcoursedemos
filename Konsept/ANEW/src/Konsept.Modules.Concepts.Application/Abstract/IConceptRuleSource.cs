using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Application.Abstract;

/// <summary>Konsept kurallarının yapılandırma değerleri (eşikler, durak kelimeler; kodda sabit değil — spec 0014 Constraints).</summary>
public interface IConceptRuleSource
{
    /// <summary>Gets kurallar.</summary>
    ConceptRuleSet Rules { get; }
}
