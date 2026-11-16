using Konsept.Modules.Concepts.Application.Resources;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Concepts.Application;

/// <summary>Concepts'in beklenen hataları (kararlı kod + Türkçe mesaj; alan hataları istekteki özellik adıyla).</summary>
public static class ConceptsErrors
{
    /// <summary>Alan: adet.</summary>
    public const string CountField = "Count";

    /// <summary>Alan: fikir.</summary>
    public const string IdeaField = "Idea";

    /// <summary>Alan: seçilen öneriler.</summary>
    public const string SuggestionsField = "SuggestionIds";

    /// <summary>Hata kodu: hazır değil (model ya da key yok, Req 1).</summary>
    public const string NotReadyCode = "concepts.not_ready";

    /// <summary>Gets oturum yok.</summary>
    public static Error SignInRequired => new("auth.required", ConceptsText.SignInRequired, ErrorType.Unauthorized);

    /// <summary>Gets profil yok ya da başkasının (BR-1).</summary>
    public static Error ProfileNotFound => new("concepts.profile_not_found", ConceptsText.ProfileNotFound, ErrorType.NotFound);

    /// <summary>Gets konsept/öneri yok ya da başkasının (BR-1, AC-28).</summary>
    public static Error NotFound => new("concepts.not_found", ConceptsText.NotFound, ErrorType.NotFound);

    /// <summary>Gets gerekli model seçilmemiş ya da key yok (Req 1, AC-1).</summary>
    public static Error NotReady => new(NotReadyCode, ConceptsText.NotReady, ErrorType.Conflict);

    /// <summary>Gets şeritte süren iş var.</summary>
    public static Error Busy => new("concepts.busy", ConceptsText.Busy, ErrorType.Conflict);

    /// <summary>Gets onay yalnızca "Üretildi"den.</summary>
    public static Error ApproveInvalidState => new("concepts.invalid_state", ConceptsText.ApproveInvalidState, ErrorType.Conflict);

    /// <summary>Gets geri alma yalnızca "Onaylandı"dan.</summary>
    public static Error UnapproveInvalidState => new("concepts.invalid_state", ConceptsText.UnapproveInvalidState, ErrorType.Conflict);

    /// <summary>Gets sırada/üretiliyor konsept düzenlenemez (spec 0015 Req 1).</summary>
    public static Error LockedQueued => new("concepts.locked", ConceptsText.LockedQueued, ErrorType.Conflict);

    /// <summary>Gets videosu olan konsept burada düzenlenemez (spec 0015 Req 1).</summary>
    public static Error LockedVideo => new("concepts.locked", ConceptsText.LockedVideo, ErrorType.Conflict);

    /// <summary>Gets konseptte süren yeniden üretim var (G-3).</summary>
    public static Error RevisionBusy => new("concepts.revision_busy", ConceptsText.RevisionBusy, ErrorType.Conflict);

    /// <summary>Gets onaylanacak hazır revizyon yok.</summary>
    public static Error RevisionNotReady => new("concepts.revision_not_ready", ConceptsText.RevisionNotReady, ErrorType.Conflict);

    /// <summary>Gets boş düzenleme talimatı.</summary>
    public static Error InstructionRequired => Field("Instruction", ConceptsText.InstructionRequired);

    /// <summary>Gets uzun düzenleme talimatı.</summary>
    public static Error InstructionTooLong => Field("Instruction", ConceptsText.InstructionTooLong);

    /// <summary>Gets sahne sayısı değişmiş ya da sahne yok (G-20).</summary>
    public static Error SceneCountInvalid => Field("Scenes", ConceptsText.SceneCountInvalid);

    /// <summary>Gets geçersiz adet.</summary>
    public static Error CountInvalid => Field(CountField, ConceptsText.CountInvalid);

    /// <summary>Gets boş fikir.</summary>
    public static Error IdeaRequired => Field(IdeaField, ConceptsText.IdeaRequired);

    /// <summary>Gets uzun fikir.</summary>
    public static Error IdeaTooLong => Field(IdeaField, ConceptsText.IdeaTooLong);

    /// <summary>Gets seçim yok.</summary>
    public static Error SelectionRequired => Field(SuggestionsField, ConceptsText.SelectionRequired);

    /// <summary>Gets geçersiz şerit.</summary>
    public static Error LaneInvalid => Field("Lane", ConceptsText.LaneInvalid);

    /// <summary>Gets geçersiz durum filtresi.</summary>
    public static Error StatusInvalid => Field("Statuses", ConceptsText.StatusInvalid);

    /// <summary>Sahne alan hataları (alan adı <c>Scenes[i].title</c> biçiminde).</summary>
    /// <param name="errors">Alan → mesaj.</param>
    /// <returns>Hata.</returns>
    public static Error SceneFields(IReadOnlyDictionary<string, string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new("concepts.invalid", ConceptsText.ValidationFailed, ErrorType.Validation)
        {
            FieldErrors = errors.ToDictionary(p => p.Key, p => new[] { p.Value }, StringComparer.Ordinal),
        };
    }

    private static Error Field(string field, string message) =>
        new("concepts.invalid", ConceptsText.ValidationFailed, ErrorType.Validation)
        {
            FieldErrors = new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] },
        };
}
