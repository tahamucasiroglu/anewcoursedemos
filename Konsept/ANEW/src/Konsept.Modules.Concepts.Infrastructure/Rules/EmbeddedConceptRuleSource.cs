using System.Text.Json;
using Konsept.Modules.Concepts.Application.Abstract;
using Konsept.Modules.Concepts.Domain.Entities;

namespace Konsept.Modules.Concepts.Infrastructure.Rules;

/// <summary>Gömülü <c>concept-rules.json</c>'dan kural değerleri (spec 0014: kodda sabit eşik yok). Bir kez okunur.</summary>
public sealed class EmbeddedConceptRuleSource : IConceptRuleSource
{
    /// <summary>Gömülü kaynağın adı.</summary>
    public const string ResourceName = "Konsept.Concepts.concept-rules.json";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly Lazy<ConceptRuleSet> Embedded = new(Load);

    /// <inheritdoc />
    public ConceptRuleSet Rules => Embedded.Value;

    private static ConceptRuleSet Load()
    {
        using var stream = typeof(EmbeddedConceptRuleSource).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Konsept kuralları derlemede yok.");
        return JsonSerializer.Deserialize<ConceptRuleSet>(stream, Json) ?? throw new InvalidOperationException("Konsept kuralları boş.");
    }
}
