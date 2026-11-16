namespace Konsept.Modules.Profiles.Domain.Rules;

/// <summary>
/// Konsept ayarı kuralları (spec 0005 Req 3–7): üç model rolü; ince ayarda her grupta tek seçim (ton 8, izleyici 4, anlatım biçimi
/// 4; <c>null</c> = "Belirtilmedi"); efor yalnızca modelin desteklediği düzeylerden biri, verilmezse katalog varsayılanı, efor
/// desteklemeyen modelde efor yoktur. Model listesi buradan değil katalogdan gelir.
/// </summary>
public static class ConceptSettingsRules
{
    /// <summary>Rol: konsept üretimi.</summary>
    public const string RoleConcept = "concept";

    /// <summary>Rol: anlamca tekrar denetimi.</summary>
    public const string RoleDedupe = "dedupe";

    /// <summary>Rol: konu önerisi.</summary>
    public const string RoleTopic = "topic";

    /// <summary>Gets roller (şeritteki sıra).</summary>
    public static IReadOnlyList<string> Roles { get; } = [RoleConcept, RoleDedupe, RoleTopic];

    /// <summary>Gets ton kodları (8).</summary>
    public static IReadOnlyList<string> Tones { get; } =
        ["mysterious", "educational", "dramatic", "calm", "documentary", "humorous", "inspiring", "questioning"];

    /// <summary>Gets izleyici kodları (4).</summary>
    public static IReadOnlyList<string> Audiences { get; } = ["beginner", "curious", "family", "expert"];

    /// <summary>Gets anlatım biçimi kodları (4; v1'de bu dördüyle sınırlı, G-12).</summary>
    public static IReadOnlyList<string> Narrations { get; } = ["single_story", "list", "myth_or_fact", "what_if"];

    /// <summary>İnce ayar değeri geçerli mi (<c>null</c> = Belirtilmedi, geçerli).</summary>
    /// <param name="allowed">Grubun kodları.</param>
    /// <param name="value">Değer.</param>
    /// <returns>Geçerliyse <c>true</c>.</returns>
    public static bool IsOption(IReadOnlyList<string> allowed, string? value) =>
        value is null || (allowed ?? []).Contains(value, StringComparer.Ordinal);

    /// <summary>Seçilen model için eforu çözer.</summary>
    /// <param name="levels">Modelin desteklediği düzeyler.</param>
    /// <param name="defaultEffort">Katalog varsayılanı.</param>
    /// <param name="requested">İstenen efor (boş = varsayılan).</param>
    /// <param name="effort">Saklanacak efor (desteklenmiyorsa <c>null</c>).</param>
    /// <returns>İstek geçerliyse <c>true</c>.</returns>
    public static bool TryResolveEffort(IReadOnlyList<string> levels, string? defaultEffort, string? requested, out string? effort)
    {
        levels ??= [];
        var wanted = string.IsNullOrWhiteSpace(requested) ? null : requested;
        if (levels.Count == 0)
        {
            effort = null;
            return wanted is null;
        }

        effort = wanted ?? (defaultEffort is not null && levels.Contains(defaultEffort, StringComparer.Ordinal) ? defaultEffort : levels[0]);
        return levels.Contains(effort, StringComparer.Ordinal);
    }
}
