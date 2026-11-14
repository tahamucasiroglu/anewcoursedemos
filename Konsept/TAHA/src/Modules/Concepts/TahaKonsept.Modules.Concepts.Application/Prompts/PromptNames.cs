namespace TahaKonsept.Modules.Concepts.Application.Prompts;

/// <summary>
/// Gömülü prompt şablonlarının adları (<c>Prompts/&lt;ad&gt;.md</c>). Şablonlar <c>{{ad}}</c> değişkeni ve
/// <c>{{#ad}}…{{/ad}}</c> koşullu bölümü kullanır (değer boşsa bölüm atlanır).
/// </summary>
public static class PromptNames
{
    /// <summary>Konsept üretimi / düzeltme sistem promptu.</summary>
    public const string ConceptSystem = "concept-system";

    /// <summary>Konsept üretimi kullanıcı promptu.</summary>
    public const string ConceptUser = "concept-user";

    /// <summary>Düzeltme kullanıcı promptu.</summary>
    public const string ReviseUser = "revise-user";

    /// <summary>Konu önerisi sistem promptu.</summary>
    public const string TopicsSystem = "topics-system";

    /// <summary>Konu önerisi kullanıcı promptu.</summary>
    public const string TopicsUser = "topics-user";

    /// <summary>Kelime tekrar denetimi sistem promptu.</summary>
    public const string WordCheckSystem = "wordcheck-system";

    /// <summary>Kelime tekrar denetimi kullanıcı promptu.</summary>
    public const string WordCheckUser = "wordcheck-user";

    /// <summary>Tüm şablon adları (test ve ön yükleme için).</summary>
    public static IReadOnlyList<string> All { get; } =
        [ConceptSystem, ConceptUser, ReviseUser, TopicsSystem, TopicsUser, WordCheckSystem, WordCheckUser];
}
