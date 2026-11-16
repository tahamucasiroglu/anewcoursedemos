namespace Konsept.Modules.Concepts.Domain.Enums;

/// <summary>Konu önerisinin durumu (spec 0014 Req 7).</summary>
public enum SuggestionState
{
    /// <summary>Listede, seçim bekliyor.</summary>
    Pending = 0,

    /// <summary>Önerilip seçilmedi (BR-4 hafızası; tekrar önerilmez).</summary>
    Rejected = 1,

    /// <summary>Seçildi ve konsept üretimine gönderildi.</summary>
    Selected = 2,
}
