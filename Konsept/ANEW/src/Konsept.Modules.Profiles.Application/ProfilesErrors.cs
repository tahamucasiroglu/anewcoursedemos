using Konsept.Modules.Profiles.Application.Resources;
using Konsept.Modules.Profiles.Domain.Enums;
using Konsept.Shared.Kernel.Enums;
using Konsept.Shared.Kernel.Results;

namespace Konsept.Modules.Profiles.Application;

/// <summary>Profiles'ın beklenen hataları (kararlı kod + Türkçe mesaj; alan hataları istekteki özellik adıyla).</summary>
public static class ProfilesErrors
{
    /// <summary>Alan: kanal adı.</summary>
    public const string ChannelNameField = "ChannelName";

    /// <summary>Alan: video dili.</summary>
    public const string LanguageField = "Language";

    /// <summary>Alan: biçim.</summary>
    public const string FormatField = "Format";

    /// <summary>Alan: hedef süre.</summary>
    public const string DurationField = "DurationSeconds";

    /// <summary>Alan: kanal içeriği.</summary>
    public const string ContentField = "Content";

    /// <summary>Alan: konsept ayarlarında model seçimleri (spec 0005).</summary>
    public const string ModelsField = "Models";

    /// <summary>Alan: konsept ayarlarında ince ayar (spec 0005).</summary>
    public const string FineTuneField = "FineTune";

    /// <summary>Hata kodu: alan kuralı.</summary>
    public const string InvalidCode = "profiles.invalid";

    /// <summary>Hata kodu: 20 profil sınırı.</summary>
    public const string LimitCode = "profiles.limit";

    /// <summary>Hata kodu: profil yok ya da kullanıcının değil (varlığı açığa çıkmaz).</summary>
    public const string NotFoundCode = "profiles.not_found";

    /// <summary>Gets profil bulunamadı (başka kullanıcının profili dahil, AC-15).</summary>
    public static Error NotFound => new(NotFoundCode, ProfilesText.NotFound, ErrorType.NotFound);

    /// <summary>Gets 20 profil sınırı (Req 3).</summary>
    public static Error LimitReached => new(LimitCode, ProfilesText.LimitReached, ErrorType.Conflict);

    /// <summary>Gets oturum yok.</summary>
    public static Error SignInRequired => new("auth.required", ProfilesText.SignInRequired, ErrorType.Unauthorized);

    /// <summary>Gets ad aynı kullanıcının başka profilinde var.</summary>
    public static Error NameTaken => Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal) { [ChannelNameField] = [ProfilesText.ChannelNameTaken] });

    /// <summary>Gets seçilemeyen model (yetki yok, key yok/okunamıyor, metin modeli değil; spec 0005 Req 5, BR-13).</summary>
    public static Error ModelNotSelectable => Field(ModelsField, ProfilesText.ModelNotSelectable);

    /// <summary>Gets model rolü geçersiz ya da tekrarlı.</summary>
    public static Error RoleInvalid => Field(ModelsField, ProfilesText.ModelRoleInvalid);

    /// <summary>Gets efor modelin desteklediği düzeylerden biri değil.</summary>
    public static Error EffortInvalid => Field(ModelsField, ProfilesText.EffortInvalid);

    /// <summary>Gets ince ayar değeri listede yok.</summary>
    public static Error FineTuneInvalid => Field(FineTuneField, ProfilesText.FineTuneInvalid);

    /// <summary>Alan kuralı ihlallerinden hata.</summary>
    /// <param name="issues">İhlaller.</param>
    /// <returns>Alan hatalı doğrulama hatası.</returns>
    public static Error FromIssues(IEnumerable<ProfileIssue> issues)
    {
        var fields = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var issue in issues)
        {
            var (field, message) = Describe(issue);
            fields[field] = [message];
        }

        return Invalid(fields);
    }

    private static Error Field(string field, string message) =>
        Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal) { [field] = [message] });

    private static Error Invalid(Dictionary<string, string[]> fields) =>
        new(InvalidCode, ProfilesText.ValidationFailed, ErrorType.Validation) { FieldErrors = fields };

    private static (string Field, string Message) Describe(ProfileIssue issue) => issue switch
    {
        ProfileIssue.NameRequired => (ChannelNameField, ProfilesText.ChannelNameRequired),
        ProfileIssue.NameTooLong => (ChannelNameField, ProfilesText.ChannelNameTooLong),
        ProfileIssue.LanguageInvalid => (LanguageField, ProfilesText.LanguageInvalid),
        ProfileIssue.FormatInvalid => (FormatField, ProfilesText.FormatInvalid),
        ProfileIssue.DurationOutOfRange => (DurationField, ProfilesText.DurationOutOfRange),
        ProfileIssue.ContentRequired => (ContentField, ProfilesText.ContentRequired),
        _ => (ContentField, ProfilesText.ContentTooLong),
    };
}
