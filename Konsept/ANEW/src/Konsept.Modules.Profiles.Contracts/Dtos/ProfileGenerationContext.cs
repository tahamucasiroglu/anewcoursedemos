namespace Konsept.Modules.Profiles.Contracts.Dtos;

/// <summary>
/// Konsept üretimi için profilin o anki değerleri (spec 0014 Req 12, 27): ad, video dili, biçim, hedef süre, sistem talimatı
/// (spec 0003 Req 16; kullanıcıya gösterilmez) ve konsept ayarları (spec 0005: rol başına model + efor, ince ayar, kelime denetimi;
/// <c>null</c> model = seçilmedi, <c>null</c> ince ayar = Belirtilmedi).
/// </summary>
public sealed record ProfileGenerationContext
{
    /// <summary>Gets profil.</summary>
    public Guid ProfileId { get; init; }

    /// <summary>Gets kanal adı.</summary>
    public required string Name { get; init; }

    /// <summary>Gets video dili (<c>tr</c> · <c>en</c>).</summary>
    public required string Language { get; init; }

    /// <summary>Gets biçim (<c>landscape</c> · <c>portrait</c>).</summary>
    public required string Format { get; init; }

    /// <summary>Gets hedef süre (saniye).</summary>
    public int DurationSeconds { get; init; }

    /// <summary>Gets profil sistem talimatı.</summary>
    public required string Instruction { get; init; }

    /// <summary>Gets konsept üretimi modeli.</summary>
    public string? ConceptModel { get; init; }

    /// <summary>Gets konsept üretimi eforu.</summary>
    public string? ConceptEffort { get; init; }

    /// <summary>Gets anlamca tekrar denetimi modeli.</summary>
    public string? DedupeModel { get; init; }

    /// <summary>Gets anlamca tekrar denetimi eforu.</summary>
    public string? DedupeEffort { get; init; }

    /// <summary>Gets konu önerisi modeli.</summary>
    public string? TopicModel { get; init; }

    /// <summary>Gets konu önerisi eforu.</summary>
    public string? TopicEffort { get; init; }

    /// <summary>Gets ton kodu.</summary>
    public string? Tone { get; init; }

    /// <summary>Gets izleyici kodu.</summary>
    public string? Audience { get; init; }

    /// <summary>Gets anlatım biçimi kodu.</summary>
    public string? Narration { get; init; }

    /// <summary>Gets a value indicating whether kelime denetimi açık (BR-15).</summary>
    public bool WordCheck { get; init; }
}
