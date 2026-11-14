namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Okunuş sözlüğü kaydının durumu: konseptten gelen öneri ya da kullanıcının onayladığı kalıcı kayıt.
/// </summary>
public enum PronunciationStatus
{
    /// <summary>Öneri (konsept üretiminde LLM önerdi); onaylanınca kalıcı olur.</summary>
    Suggested = 0,

    /// <summary>Onaylı; Normalize aşamasında her zaman uygulanır.</summary>
    Approved = 1,
}
