using Konsept.Shared.Kernel.Security;
using Serilog.Core;
using Serilog.Events;

namespace Konsept.Shared.Logging;

/// <summary>
/// Log olayının özelliklerini maskeler (spec 0008 Req 8): adı gizli olan özellik alan türüne göre (şifre → tamamen,
/// API key → <c>sk-…a3f</c>), diğer metin özellikleri serbest metin desenleriyle; iç içe nesnelere ve listelere de
/// iner. Serilog sözleşmesini uygulayan çerçeve adaptörüdür; mantık <see cref="SecretMasker"/>'dadır. Zincirdeki
/// son enricher olmalıdır.
/// </summary>
public sealed class MaskingEnricher : ILogEventEnricher
{
    /// <inheritdoc />
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        foreach (var property in logEvent.Properties.ToList())
        {
            var masked = MaskValue(property.Key, property.Value);
            if (!ReferenceEquals(masked, property.Value))
            {
                logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, masked));
            }
        }
    }

    private static LogEventPropertyValue MaskValue(string name, LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: string text } => Replace(value, SecretMasker.MaskField(name, text)),
        ScalarValue { Value: null } => value,
        ScalarValue scalar when SecretMasker.IsSecretField(name) => new ScalarValue(SecretMasker.MaskField(name, scalar.Value.ToString())),

        // Gizli ad altındaki iç içe değer (ör. {@ApiKey} → { Value = … }, {Passwords} listesi) tümüyle maskelenir (hunt).
        StructureValue or SequenceValue or DictionaryValue when SecretMasker.IsSecretField(name) => MaskAllUnder(name, value),
        StructureValue structure => MaskStructure(structure),
        SequenceValue sequence => MaskSequence(name, sequence),
        DictionaryValue dictionary => MaskDictionary(dictionary),
        _ => value,
    };

    private static LogEventPropertyValue MaskAllUnder(string secretName, LogEventPropertyValue value) => value switch
    {
        ScalarValue { Value: null } => value,
        ScalarValue scalar => new ScalarValue(SecretMasker.MaskField(secretName, scalar.Value.ToString())),
        StructureValue structure => new StructureValue(
            [.. structure.Properties.Select(p => new LogEventProperty(p.Name, MaskAllUnder(secretName, p.Value)))],
            structure.TypeTag),
        SequenceValue sequence => new SequenceValue([.. sequence.Elements.Select(e => MaskAllUnder(secretName, e))]),
        DictionaryValue dictionary => new DictionaryValue(
            [.. dictionary.Elements.Select(e => new KeyValuePair<ScalarValue, LogEventPropertyValue>(e.Key, MaskAllUnder(secretName, e.Value)))]),
        _ => value,
    };

    private static LogEventPropertyValue Replace(LogEventPropertyValue original, string masked) =>
        original is ScalarValue { Value: string text } && text == masked ? original : new ScalarValue(masked);

    private static LogEventPropertyValue MaskStructure(StructureValue structure)
    {
        var properties = structure.Properties.Select(p => new LogEventProperty(p.Name, MaskValue(p.Name, p.Value))).ToList();
        return properties.Zip(structure.Properties).All(pair => ReferenceEquals(pair.First.Value, pair.Second.Value))
            ? structure
            : new StructureValue(properties, structure.TypeTag);
    }

    private static LogEventPropertyValue MaskSequence(string name, SequenceValue sequence)
    {
        var elements = sequence.Elements.Select(e => MaskValue(name, e)).ToList();
        return elements.Zip(sequence.Elements).All(pair => ReferenceEquals(pair.First, pair.Second))
            ? sequence
            : new SequenceValue(elements);
    }

    private static LogEventPropertyValue MaskDictionary(DictionaryValue dictionary)
    {
        var entries = dictionary.Elements
            .Select(e => new KeyValuePair<ScalarValue, LogEventPropertyValue>(e.Key, MaskValue(e.Key.Value?.ToString() ?? string.Empty, e.Value)))
            .ToList();
        return entries.Zip(dictionary.Elements).All(pair => ReferenceEquals(pair.First.Value, pair.Second.Value))
            ? dictionary
            : new DictionaryValue(entries);
    }
}
