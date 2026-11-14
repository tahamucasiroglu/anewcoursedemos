using TahaKonsept.Modules.Concepts.Domain.Constants;
using TahaKonsept.Modules.Concepts.Domain.ValueObjects;
using TahaKonsept.Shared.Contracts.Concepts;

namespace TahaKonsept.Modules.Concepts.Application.Models;

/// <summary>
/// Bir üretim / düzeltme çağrısının bağlamı (bir iş boyunca sabit → sistem promptu önbelleğe uygun): kullanıcı, profil,
/// profilden üretilmiş kanal metni (<see cref="ProfileBlock"/>), tercihler, dil/biçim/süre, etkin ton-izleyici-anlatım,
/// gerçek bilgi bayrağı ve seslendirme hızı. <see cref="TargetWords"/> ve <see cref="SceneCountHint"/> türetilir.
/// </summary>
public sealed record ConceptGenerationContext
{
    /// <summary>Kullanıcı.</summary>
    public Guid UserId { get; init; }

    /// <summary>Profil.</summary>
    public Guid? ProfileId { get; init; }

    /// <summary>Profilin prompt metni (<c>IProfilePromptBuilder</c>).</summary>
    public string ProfileBlock { get; init; } = string.Empty;

    /// <summary>Tercihler.</summary>
    public ConceptPreferences Preferences { get; init; } = ConceptPreferences.Default;

    /// <summary>Video dili.</summary>
    public string Language { get; init; } = "tr";

    /// <summary>Biçim.</summary>
    public VideoFormat Format { get; init; } = VideoFormat.Vertical;

    /// <summary>Hedef süre (saniye).</summary>
    public int TargetDurationSec { get; init; } = 60;

    /// <summary>Etkin ton (ayar ya da profil).</summary>
    public string Tone { get; init; } = string.Empty;

    /// <summary>Etkin izleyici.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>Etkin anlatım biçimi.</summary>
    public string NarrativeStyle { get; init; } = string.Empty;

    /// <summary>Gerçek bilgi (web araması + kaynak zorunlu).</summary>
    public bool FactGrounding { get; init; }

    /// <summary>Seslendirme hızı (kelime/sn).</summary>
    public double WordsPerSecond { get; init; } = ConceptLimits.TurkishWordsPerSecond;

    /// <summary>Hedef toplam seslendirme kelime sayısı.</summary>
    public int TargetWords => (int)Math.Round(TargetDurationSec * WordsPerSecond);

    /// <summary>Önerilen sahne sayısı (~7 sn/sahne, 3–60).</summary>
    public int SceneCountHint => Math.Clamp((int)Math.Round(TargetDurationSec / 7.0), 3, ConceptLimits.MaxScenes);
}
