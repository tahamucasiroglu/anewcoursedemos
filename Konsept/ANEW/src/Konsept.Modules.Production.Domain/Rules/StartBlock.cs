using Konsept.Modules.Production.Domain.Entities;

namespace Konsept.Modules.Production.Domain.Rules;

/// <summary>
/// "Sırayla üret"in pasif olma nedeni (spec 0006 Req 6, 14; AC-4, AC-6, AC-15, AC-17, AC-22). İlk geçerli neden döner; hiçbiri yoksa
/// <c>null</c>. Dil kontrolü sıradaki (<i>Sırada</i>) işlerin konseptlerinin <b>kaydındaki</b> dille yapılır; profilin güncel dili kullanılmaz.
/// </summary>
public static class StartBlock
{
    /// <summary>Sıra boş.</summary>
    public const string Empty = "empty";

    /// <summary>Bu profilde üretim sürüyor.</summary>
    public const string RunningHere = "running_here";

    /// <summary>Başka profilde üretim sürüyor.</summary>
    public const string RunningElsewhere = "running_elsewhere";

    /// <summary>Model listesi yüklenemedi.</summary>
    public const string ModelsFailed = "models_failed";

    /// <summary>Video kodu modeli seçilmedi.</summary>
    public const string NoCodeModel = "no_code_model";

    /// <summary>Seçili model artık listede yok (ör. Remotion kaldırıldı).</summary>
    public const string Unavailable = "unavailable";

    /// <summary>Seçili modellerden birine yetki yok.</summary>
    public const string NoPermission = "no_permission";

    /// <summary>Seçili online modelin API key'i yok.</summary>
    public const string NoKey = "no_key";

    /// <summary>Ses modeli konsept dilini desteklemiyor.</summary>
    public const string AudioLanguage = "audio_language";

    /// <summary>Görsel modeli konsept dilini desteklemiyor.</summary>
    public const string ImageLanguage = "image_language";

    /// <summary>Dil uyarıları (modal ve özet yanında; Req 6).</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="models">Listelenen modeller.</param>
    /// <param name="queuedLanguages">Sırada işlerin konsept dilleri.</param>
    /// <returns>Uyarı kodları (<see cref="AudioLanguage"/>, <see cref="ImageLanguage"/>).</returns>
    public static IReadOnlyList<string> LanguageWarnings(
        ProductionSettings settings, IReadOnlyList<SelectableModel>? models, IReadOnlyCollection<string> queuedLanguages)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(queuedLanguages);
        var warnings = new List<string>();
        if (Mismatch(settings.AudioModel, models, queuedLanguages))
        {
            warnings.Add(AudioLanguage);
        }

        if (Mismatch(settings.ImageModel, models, queuedLanguages))
        {
            warnings.Add(ImageLanguage);
        }

        return warnings;
    }

    /// <summary>Pasiflik nedeni.</summary>
    /// <param name="settings">Ayarlar.</param>
    /// <param name="models">Listelenen modeller; yüklenemediyse <c>null</c>.</param>
    /// <param name="queuedLanguages">Bu profilin <i>Sırada</i> işlerinin konsept dilleri (boşsa sıra boş).</param>
    /// <param name="runningHere">Bu profilde çalışan iş var.</param>
    /// <param name="runningElsewhere">Kullanıcının başka profilinde çalışan iş var.</param>
    /// <returns>Neden kodu ya da <c>null</c>.</returns>
    public static string? Evaluate(
        ProductionSettings settings,
        IReadOnlyList<SelectableModel>? models,
        IReadOnlyCollection<string> queuedLanguages,
        bool runningHere,
        bool runningElsewhere)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(queuedLanguages);
        if (runningHere)
        {
            return RunningHere;
        }

        if (runningElsewhere)
        {
            return RunningElsewhere;
        }

        if (queuedLanguages.Count == 0)
        {
            return Empty;
        }

        if (models is null)
        {
            return ModelsFailed;
        }

        if (settings.CodeModel is null)
        {
            return NoCodeModel;
        }

        var chosen = new[] { settings.CodeModel, settings.Engine, settings.ImageModel, settings.AudioModel }
            .OfType<string>()
            .Select(id => models.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.Ordinal)))
            .ToList();
        if (chosen.Any(m => m is null))
        {
            return Unavailable;
        }

        if (chosen.Any(m => m!.Availability == "no_permission"))
        {
            return NoPermission;
        }

        if (chosen.Any(m => m!.Availability is "no_key" or "unreadable"))
        {
            return NoKey;
        }

        return LanguageWarnings(settings, models, queuedLanguages).FirstOrDefault();
    }

    private static bool Mismatch(string? modelId, IReadOnlyList<SelectableModel>? models, IReadOnlyCollection<string> languages)
    {
        var only = modelId is null ? null : models?.FirstOrDefault(m => string.Equals(m.Id, modelId, StringComparison.Ordinal))?.LanguageOnly;
        return only is not null && languages.Any(l => !string.Equals(l, only, StringComparison.OrdinalIgnoreCase));
    }
}
