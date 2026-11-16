namespace Konsept.Modules.AiGateway.Contracts.Enums;

/// <summary>Başka modülün yaptığı metin çağrısının model rolü (harcama kaydında amaç olarak saklanır; BR-12, spec 0014 Req 16).</summary>
public enum ModelRole
{
    /// <summary>Konu önerisi.</summary>
    TopicSuggestion,

    /// <summary>Konsept üretimi (BR-15 yeniden üretimi dahil).</summary>
    ConceptGeneration,

    /// <summary>Anlamca tekrar denetimi.</summary>
    DuplicateCheck,
}
