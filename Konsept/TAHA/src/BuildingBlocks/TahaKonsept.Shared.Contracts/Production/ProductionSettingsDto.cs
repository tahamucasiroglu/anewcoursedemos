using TahaKonsept.Shared.Contracts.Providers;

namespace TahaKonsept.Shared.Contracts.Production;

/// <summary>
/// Kullanıcının video üretim ayarları (<c>GET|PUT api/production/settings</c>); iş başlarken anlık görüntüsü işe kopyalanır.
/// Önemli alanlar: <see cref="CodeModelKey"/> + <see cref="CodeEffort"/> (sahne kodu LLM'i), <see cref="VideoEngine"/>, <see cref="ImageMode"/>/<see cref="ImageModelKey"/>,
/// <see cref="SpeechMode"/>/<see cref="SpeechModelKey"/>/<see cref="VoiceId"/>/<see cref="SpeechSpeed"/>, <see cref="NormalizeBySpeechModel"/> (motor başına okunuş düzeltme),
/// <see cref="BurnSubtitles"/>, <see cref="ShowProgressBar"/>, <see cref="Fps"/>. Çözünürlük profil/konsept biçiminden gelir (dikey 1080x1920, yatay 1920x1080).
/// </summary>
public sealed record ProductionSettingsDto
{
    /// <summary>Sahne kodunu yazan LLM'in katalog anahtarı.</summary>
    public string CodeModelKey { get; init; } = "claude-opus-5-5";

    /// <summary>Efor seviyesi (<c>null</c> = modelin varsayılanı).</summary>
    public EffortLevel? CodeEffort { get; init; }

    /// <summary>Video motoru: <c>hyperframes</c>, <c>remotion</c> ya da <c>revideo</c>.</summary>
    public string VideoEngine { get; init; } = "hyperframes";

    /// <summary>Görsel kipi.</summary>
    public MediaMode ImageMode { get; init; } = MediaMode.Off;

    /// <summary>Görsel modeli katalog anahtarı (yerel: <c>z-image-turbo</c>; online: <c>gemini-nano-banana-2.1</c> ...).</summary>
    public string? ImageModelKey { get; init; }

    /// <summary>Ses kipi (kapalı = sessiz video).</summary>
    public MediaMode SpeechMode { get; init; } = MediaMode.Local;

    /// <summary>Ses modeli katalog anahtarı (yerel: <c>supertonic-3</c>; online: <c>gemini-3.8-flash-tts</c> ...).</summary>
    public string? SpeechModelKey { get; init; } = "supertonic-3";

    /// <summary>Ses/konuşmacı kimliği (motora özgü; <c>null</c> = varsayılan).</summary>
    public string? VoiceId { get; init; }

    /// <summary>Konuşma hızı çarpanı (1.0 normal).</summary>
    public double SpeechSpeed { get; init; } = 1.0;

    /// <summary>Ses klonlama için referans ses artefaktı (ileride; şimdilik kullanılmaz).</summary>
    public Guid? ReferenceAudioArtifactId { get; init; }

    /// <summary>Ses modeli anahtarı → okunuş düzeltme açık mı. Listede olmayan model için açık kabul edilir.</summary>
    public IReadOnlyDictionary<string, bool> NormalizeBySpeechModel { get; init; } = new Dictionary<string, bool>();

    /// <summary>Karaoke altyazı basılsın mı.</summary>
    public bool BurnSubtitles { get; init; } = true;

    /// <summary>Üstte ilerleme çubuğu olsun mu.</summary>
    public bool ShowProgressBar { get; init; } = true;

    /// <summary>Kare hızı.</summary>
    public int Fps { get; init; } = 30;
}
